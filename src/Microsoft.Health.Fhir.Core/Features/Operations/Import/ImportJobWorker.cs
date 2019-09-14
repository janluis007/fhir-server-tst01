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
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    /// <summary>
    /// The worker responsible for running the import job tasks.
    /// </summary>
    public class ImportJobWorker
    {
        private readonly Func<IScoped<IFhirOperationDataStore>> _fhirOperationDataStoreFactory;
        private readonly ImportJobConfiguration _importJobConfiguration;
        private readonly IImportJobTaskFactory _importJobTaskFactory;
        private readonly ILogger _logger;

        public ImportJobWorker(Func<IScoped<IFhirOperationDataStore>> fhirOperationDataStoreFactory, IOptions<ImportJobConfiguration> importJobConfiguration, IImportJobTaskFactory importJobTaskFactory, ILogger<ImportJobWorker> logger)
        {
            EnsureArg.IsNotNull(fhirOperationDataStoreFactory, nameof(fhirOperationDataStoreFactory));
            EnsureArg.IsNotNull(importJobConfiguration?.Value, nameof(importJobConfiguration));
            EnsureArg.IsNotNull(importJobTaskFactory, nameof(importJobTaskFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _fhirOperationDataStoreFactory = fhirOperationDataStoreFactory;
            _importJobConfiguration = importJobConfiguration.Value;
            _importJobTaskFactory = importJobTaskFactory;
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
                        using (IScoped<IFhirOperationDataStore> store = _fhirOperationDataStoreFactory())
                        {
                            IReadOnlyCollection<ImportJobOutcome> jobs = await store.Value.AcquireImportJobsAsync(
                                _importJobConfiguration.MaximumNumberOfConcurrentJobsAllowed,
                                _importJobConfiguration.JobHeartbeatTimeoutThreshold,
                                cancellationToken);

                            runningTasks.AddRange(jobs.Select(job => _importJobTaskFactory.Create(job.JobRecord, job.ETag, cancellationToken)));
                        }
                    }

                    await Task.Delay(_importJobConfiguration.JobPollingFrequency, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // The worker is canceled.
                }
                catch (Exception ex)
                {
                    // The job failed.
                    _logger.LogError(ex, "Unhandled exception in the worker.");
                }
            }
        }
    }
}
