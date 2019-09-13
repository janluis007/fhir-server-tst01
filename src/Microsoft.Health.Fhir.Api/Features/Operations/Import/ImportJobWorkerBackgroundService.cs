// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Fhir.Core.Features.Operations.Import;

namespace Microsoft.Health.Fhir.Api.Features.Operations.Import
{
    /// <summary>
    /// The background service used to host the <see cref="ImportJobWorker"/>.
    /// </summary>
    public class ImportJobWorkerBackgroundService : BackgroundService
    {
        private readonly ImportJobWorker _importJobWorker;

        public ImportJobWorkerBackgroundService(ImportJobWorker importJobWorker)
        {
            EnsureArg.IsNotNull(importJobWorker, nameof(importJobWorker));

            _importJobWorker = importJobWorker;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _importJobWorker.ExecuteAsync(stoppingToken);
        }
    }
}
