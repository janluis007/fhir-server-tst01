// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    public sealed class ImportJobTask : IDisposable
    {
        private static readonly int NewLineLength = "\n".Length;

        private readonly ImportJobRecord _importJobRecord;
        private readonly ImportJobConfiguration _importJobConfiguration;
        private readonly IFhirOperationDataStore _fhirOperationDataStore;
        private readonly IImportProvider _importProvider;
        private readonly IResourceWrapperFactory _resourceWrapperFactory;
        private readonly IFhirDataStore _fhirDataStore;
        private readonly FhirJsonParser _fhirJsonParser;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly int _bufferSize;

        private WeakETag _weakETag;

        public ImportJobTask(
            ImportJobRecord importJobRecord,
            WeakETag weakETag,
            ImportJobConfiguration importJobConfiguration,
            IFhirOperationDataStore fhirOperationDataStore,
            IImportProvider importProvider,
            IResourceWrapperFactory resourceWrapperFactory,
            IFhirDataStore fhirDataStore,
            FhirJsonParser fhirParser,
            ILogger<ImportJobTask> logger)
        {
            EnsureArg.IsNotNull(importJobRecord, nameof(importJobRecord));
            EnsureArg.IsNotNull(weakETag, nameof(weakETag));
            EnsureArg.IsNotNull(importJobConfiguration, nameof(importJobConfiguration));
            EnsureArg.IsNotNull(fhirOperationDataStore, nameof(fhirOperationDataStore));
            EnsureArg.IsNotNull(importProvider, nameof(importProvider));
            EnsureArg.IsNotNull(resourceWrapperFactory, nameof(resourceWrapperFactory));
            EnsureArg.IsNotNull(fhirDataStore, nameof(fhirDataStore));
            EnsureArg.IsNotNull(fhirParser, nameof(fhirParser));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _importJobRecord = importJobRecord;
            _importJobConfiguration = importJobConfiguration;
            _fhirOperationDataStore = fhirOperationDataStore;
            _importProvider = importProvider;
            _resourceWrapperFactory = resourceWrapperFactory;
            _fhirDataStore = fhirDataStore;
            _fhirJsonParser = fhirParser;
            _logger = logger;

            _bufferSize = _importJobConfiguration.BufferSizeInMbytes * 1024;
            _weakETag = weakETag;
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Try to acquire the job.
                try
                {
                    _logger.LogTrace("Acquiring the job.");

                    await UpdateJobStatus(OperationStatus.Running, cancellationToken);
                }
                catch (JobConflictException)
                {
                    // The job is taken by another process.
                    _logger.LogWarning("Failed to acquire the job. The job was acquired by another process.");
                    return;
                }

                var tasks = new List<Task>();
                int i = 0;

                // Find the entry to work on.
                while (i < _importJobRecord.Request.Input.Count)
                {
                    ImportRequestEntry input = _importJobRecord.Request.Input[i];

                    // Get the current progress. If there is none, create one.
                    ImportJobProgress progress = null;

                    if (_importJobRecord.Progress.Count <= i)
                    {
                        progress = new ImportJobProgress();

                        _importJobRecord.Progress.Add(progress);
                    }
                    else
                    {
                        progress = _importJobRecord.Progress[i];

                        if (progress.IsComplete)
                        {
                            continue;
                        }
                    }

                    var task = Task.Run(() => ProcessInput(input, progress, cancellationToken));

                    tasks.Add(task);
                    i++;

                    if (tasks.Count == _importJobConfiguration.MaximumNumberOfConcurrentTaskPerJob)
                    {
                        Task completedTask = await Task.WhenAny(tasks);

                        if (completedTask.IsFaulted)
                        {
                            _importJobRecord.Errors.Add(new OperationOutcome()
                            {
                                Issue = new System.Collections.Generic.List<OperationOutcome.IssueComponent>()
                                    {
                                        new OperationOutcome.IssueComponent()
                                        {
                                            Diagnostics = completedTask.Exception.ToString(),
                                        },
                                    },
                            });
                        }

                        await UpdateJobStatus(OperationStatus.Running, cancellationToken);

                        tasks.Remove(completedTask);
                    }
                }

                ////while (tasks.Count < _importJobConfiguration.MaximumNumberOfConcurrentTaskPerJob &&
                ////    _importJobRecord.Request.Input.Count < i)
                ////{
                ////}

                ////for (int i = 0; i < _importJobRecord.Request.Input.Count; i++)
                ////{
                ////    await UpdateJobStatus(OperationStatus.Running, cancellationToken);
                ////}

                // We have acquired the job, process the export.
                _logger.LogTrace("Successfully completed the job.");

                _importJobRecord.EndTime = DateTimeOffset.UtcNow;

                await UpdateJobStatus(OperationStatus.Completed, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The job is being terminated.
            }
            catch (Exception ex)
            {
                // The job has encountered an error it cannot recover from.
                // Try to update the job to failed state.
                _logger.LogError(ex, "Encountered an unhandled exception. The job will be marked as failed.");

                _importJobRecord.EndTime = DateTimeOffset.UtcNow;

                await UpdateJobStatus(OperationStatus.Failed, cancellationToken);
            }
        }

        private async Task UpdateJobStatus(OperationStatus operationStatus, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync();

            try
            {
                _importJobRecord.Status = operationStatus;

                ImportJobOutcome updatedImportJobOutcome = await _fhirOperationDataStore.UpdateImportJobAsync(_importJobRecord, _weakETag, cancellationToken);

                _weakETag = updatedImportJobOutcome.ETag;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ProcessInput(ImportRequestEntry input, ImportJobProgress progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Process the file.
            var blobUrl = new Uri(input.Url);

            bool isComplete = false;

            do
            {
                StreamReader streamReader = await _importProvider.DownloadRangeToStreamReaderAsync(blobUrl, progress.BytesProcessed, _bufferSize, cancellationToken);

                isComplete = streamReader.BaseStream.Length < _bufferSize;

                string line;

                do
                {
                    line = await streamReader.ReadLineAsync();

                    if (streamReader.EndOfStream && !isComplete)
                    {
                        // We have reached the end. Commit up to this point.
                        await UpdateJobStatus(OperationStatus.Running, cancellationToken);

                        break;
                    }
                    else
                    {
                        // Upsert the resource.
                        try
                        {
                            Resource resource = _fhirJsonParser.Parse<Resource>(line);

                            ResourceWrapper wrapper = _resourceWrapperFactory.Create(resource, false);

                            await _fhirDataStore.UpsertAsync(wrapper, null, true, true, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _importJobRecord.Errors.Add(new OperationOutcome()
                            {
                                Issue = new System.Collections.Generic.List<OperationOutcome.IssueComponent>()
                                    {
                                        new OperationOutcome.IssueComponent()
                                        {
                                            Diagnostics = ex.ToString(),
                                        },
                                    },
                            });
                        }

                        progress.Count++;

                        // Increment the number of bytes processed (including the new line).
                        progress.BytesProcessed += Encoding.UTF8.GetBytes(line).Length + NewLineLength;
                    }
                }
                while (!streamReader.EndOfStream);
            }
            while (!isComplete);

            progress.IsComplete = true;
        }
    }
}
