// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    /// <summary>
    /// The worker responsible for running the import job tasks.
    /// </summary>
    public class ImportJobWorker
    {
        private readonly IFhirOperationDataStore _fhirOperationDataStore;
        private readonly ImportJobConfiguration _importJobConfiguration;
        private readonly IImportJobTaskFactory _importobTaskFactory;
        private readonly ILogger _logger;

        public ImportJobWorker(IFhirOperationDataStore fhirOperationsDataStore, IOptions<ImportJobConfiguration> exportJobConfiguration, IImportJobTaskFactory exportJobTaskFactory, ILogger<ImportJobWorker> logger)
        {
            EnsureArg.IsNotNull(fhirOperationsDataStore, nameof(fhirOperationsDataStore));
            EnsureArg.IsNotNull(exportJobConfiguration?.Value, nameof(exportJobConfiguration));
            EnsureArg.IsNotNull(exportJobTaskFactory, nameof(exportJobTaskFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _fhirOperationDataStore = fhirOperationsDataStore;
            _importJobConfiguration = exportJobConfiguration.Value;
            _importobTaskFactory = exportJobTaskFactory;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var runningTasks = new List<Task>();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Remove all completed tasks.
                    runningTasks.RemoveAll(task => task.IsCompleted);

                    // Get list of available jobs.
                    if (runningTasks.Count < _importJobConfiguration.MaximumNumberOfConcurrentJobsAllowed)
                    {
                        IReadOnlyCollection<ImportJobOutcome> jobs = await _fhirOperationDataStore.AcquireImportJobsAsync(
                            _importJobConfiguration.MaximumNumberOfConcurrentJobsAllowed,
                            _importJobConfiguration.JobHeartbeatTimeoutThreshold,
                            cancellationToken);

                        runningTasks.AddRange(jobs.Select(job => _importobTaskFactory.Create(job.JobRecord, job.ETag, cancellationToken)));
                    }

                    await Task.Delay(_importJobConfiguration.JobPollingFrequency);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
                {
                    // The job failed.
                    _logger.LogError(ex, "Unhandled exception in the worker.");
                }
            }
        }
    }
}
