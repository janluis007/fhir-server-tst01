// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;
using Microsoft.Health.Fhir.Core.Features.Persistence;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    public class ImportJobTask
    {
        private readonly ImportJobRecord _importJobRecord;
        private readonly IFhirOperationDataStore _fhirOperationDataStore;
        private readonly ILogger _logger;

        private WeakETag _weakETag;

        public ImportJobTask(
            ImportJobRecord importJobRecord,
            WeakETag weakETag,
            IFhirOperationDataStore fhirOperationDataStore,
            ILogger<ImportJobTask> logger)
        {
            EnsureArg.IsNotNull(importJobRecord, nameof(importJobRecord));
            EnsureArg.IsNotNull(weakETag, nameof(weakETag));
            EnsureArg.IsNotNull(fhirOperationDataStore, nameof(fhirOperationDataStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _importJobRecord = importJobRecord;
            _fhirOperationDataStore = fhirOperationDataStore;
            _logger = logger;

            _weakETag = weakETag;
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

                // We have acquired the job, process the export.
                _logger.LogTrace("Successfully completed the job.");

                await UpdateJobStatus(OperationStatus.Completed, cancellationToken);
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
            _importJobRecord.Status = operationStatus;

            ImportJobOutcome updatedImportJobOutcome = await _fhirOperationDataStore.UpdateImportJobAsync(_importJobRecord, _weakETag, cancellationToken);

            _weakETag = updatedImportJobOutcome.ETag;
        }
    }
}
