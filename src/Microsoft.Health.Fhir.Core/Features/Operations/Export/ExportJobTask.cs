// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Core.Features.Operations.Export.ExportDestinationClient;
using Microsoft.Health.Fhir.Core.Features.Operations.Export.Models;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.SecretStore;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export
{
    public class ExportJobTask : IExportJobTask
    {
        private readonly IFhirOperationDataStore _fhirOperationDataStore;
        private readonly ISecretStore _secretStore;
        private readonly IExportExecutor _exportExecutor;
        private readonly ILogger _logger;

        // Currently we will have only one file per resource type. In the future we will add the ability to split
        // individual files based on a max file size. This could result in a single resource having multiple files.
        // We will have to update the below mapping to support multiple ExportFileInfo per resource type.
        private readonly Dictionary<string, ExportFileInfo> _resourceTypeToFileInfoMapping = new Dictionary<string, ExportFileInfo>();

        private ExportJobRecord _exportJobRecord;
        private WeakETag _weakETag;

        public ExportJobTask(
            IFhirOperationDataStore fhirOperationDataStore,
            ISecretStore secretStore,
            IExportExecutor exportExecutor,
            ILogger<ExportJobTask> logger)
        {
            EnsureArg.IsNotNull(fhirOperationDataStore, nameof(fhirOperationDataStore));
            EnsureArg.IsNotNull(secretStore, nameof(secretStore));
            EnsureArg.IsNotNull(exportExecutor, nameof(exportExecutor));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _fhirOperationDataStore = fhirOperationDataStore;
            _secretStore = secretStore;
            _exportExecutor = exportExecutor;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(ExportJobRecord exportJobRecord, WeakETag weakETag, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(exportJobRecord, nameof(exportJobRecord));

            _exportJobRecord = exportJobRecord;
            _weakETag = weakETag;

            try
            {
                // We have acquired the job, process the export.

                // Get destination type from secret store.
                // DestinationInfo destInfo = await GetDestinationInfo();

                // TODO: This uses a mock destination client. Will be replaced by an actual destination client later.
                // Get the appropriate destination client from the client factory and connect to destination.
                var mockDestinationClient = ExportDestinationClientFactory.Instance.GetExportDestinationClient("Mock");
                await mockDestinationClient.ConnectAsync("destinationConnection");

                // Start exporting the resources by using the ExportExecutor.
                while (true)
                {
                    GetExportDataResult result = await _exportExecutor.GetExportDataAsync(_exportJobRecord.CreateExportRequest, exportJobRecord.Progress);

                    if (result.Resources.Count > 0)
                    {
                        Dictionary<Uri, List<Resource>> resourcesToSend = await ProcessResources(result.Resources, mockDestinationClient);

                        // Send list of resources and the corresponding files they need to be committed to to the destination client.
                        await mockDestinationClient.CommitAsync(resourcesToSend);
                    }

                    // Update the job record and store it in the data store.
                    int page = 1;
                    if (_exportJobRecord.Progress != null)
                    {
                        page += _exportJobRecord.Progress.Page;
                    }

                    _exportJobRecord.Progress = new ExportJobProgress(result.ContinuationToken, page);

                    // TODO: Optimize the process of updating the Output without having to replace the whole list every time.
                    _exportJobRecord.Output.Clear();
                    _exportJobRecord.Output.AddRange(_resourceTypeToFileInfoMapping.Values);

                    await UpdateJobRecord(_exportJobRecord, cancellationToken);

                    // Check whether we have reached the end of the search.
                    if (string.IsNullOrWhiteSpace(result.ContinuationToken))
                    {
                        break;
                    }
                }

                _logger.LogTrace("Successfully completed the job.");

                await UpdateJobStatus(OperationStatus.Completed, cancellationToken);
                await _secretStore.DeleteSecretAsync(_exportJobRecord.SecretName);
            }
            catch (JobConflictException)
            {
                // The job was updated by another process.
                _logger.LogWarning("The job was updated by another process.");

                // TODO: We will want to get the latest and merge the results without updating the status.
                return;
            }
            catch (Exception ex)
            {
                // The job has encountered an error it cannot recover from.
                // Try to update the job to failed state.
                _logger.LogError(ex, "Encountered an unhandled exception. The job will be marked as failed.");

                await UpdateJobStatus(OperationStatus.Failed, cancellationToken);
            }
        }

        private async Task UpdateJobStatus(OperationStatus operationStatus, CancellationToken cancellationToken)
        {
            _exportJobRecord.Status = operationStatus;

            await UpdateJobRecord(_exportJobRecord, cancellationToken);
        }

        private async Task UpdateJobRecord(ExportJobRecord jobRecord, CancellationToken cancellationToken)
        {
            ExportJobOutcome updatedExportJobOutcome = await _fhirOperationDataStore.UpdateExportJobAsync(jobRecord, _weakETag, cancellationToken);

            _exportJobRecord = updatedExportJobOutcome.JobRecord;
            _weakETag = updatedExportJobOutcome.ETag;
        }

        private async Task<DestinationInfo> GetDestinationInfo()
        {
            SecretWrapper secret = await _secretStore.GetSecretAsync(_exportJobRecord.SecretName);

            DestinationInfo destinationInfo = JsonConvert.DeserializeObject<DestinationInfo>(secret.SecretValue);
            return destinationInfo;
        }

        /// <summary>
        /// Given a list of resources, this method looks at the resource type and determines which file it should
        /// be saved to. If no file exists for a given resource, it will use the <paramref name="destinationClient"/> to create a new file.
        /// </summary>
        /// <param name="resources">List of resources to process.</param>
        /// <param name="destinationClient">Client used to connect to a destination.</param>
        /// <returns>A mapping of resources and the file they should be saved to.</returns>
        private async Task<Dictionary<Uri, List<Resource>>> ProcessResources(List<Resource> resources, IExportDestinationClient destinationClient)
        {
            var fileToResourcesMapping = new Dictionary<Uri, List<Resource>>();
            foreach (Resource resource in resources)
            {
                string resourceType = resource.ResourceType.ToString();

                // Check whether we already have an exisiting file for the current resource type.
                ExportFileInfo exportFileInfo;
                if (!_resourceTypeToFileInfoMapping.TryGetValue(resourceType, out exportFileInfo))
                {
                    string fileName = resourceType + ".ndjson";
                    Uri fileUri = await destinationClient.CreateNewFileAsync(fileName);

                    exportFileInfo = new ExportFileInfo(resourceType, fileUri, sequence: 0, count: 0, committedBytes: 0);
                    _resourceTypeToFileInfoMapping.Add(resourceType, exportFileInfo);
                }

                // Add current resource to the list of resources for the appropriate resource type.
                List<Resource> existingResources;
                if (!fileToResourcesMapping.TryGetValue(exportFileInfo.FileUri, out existingResources))
                {
                    existingResources = new List<Resource>();
                    fileToResourcesMapping.Add(exportFileInfo.FileUri, existingResources);
                }

                existingResources.Add(resource);
                exportFileInfo.Count++;
            }

            return fileToResourcesMapping;
        }
    }
}
