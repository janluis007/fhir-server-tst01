// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;
using Microsoft.Health.Fhir.Core.Features.Persistence;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    /// <summary>
    /// Provides mechanism to create a new import job task.
    /// </summary>
    public class ImportJobTaskFactory : IImportJobTaskFactory
    {
        private readonly IFhirOperationDataStore _fhirOperationDataStore;
        private readonly ILoggerFactory _loggerFactory;

        public ImportJobTaskFactory(
            IFhirOperationDataStore fhirOperationDataStore,
            ILoggerFactory loggerFactory)
        {
            EnsureArg.IsNotNull(fhirOperationDataStore, nameof(fhirOperationDataStore));
            EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory));

            _fhirOperationDataStore = fhirOperationDataStore;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public Task Create(ImportJobRecord importJobRecord, WeakETag weakETag, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(importJobRecord, nameof(importJobRecord));

            var exportJobTask = new ImportJobTask(
                importJobRecord,
                weakETag,
                _fhirOperationDataStore,
                _loggerFactory.CreateLogger<ImportJobTask>());

            using (ExecutionContext.SuppressFlow())
            {
                return Task.Run(async () => await exportJobTask.ExecuteAsync(cancellationToken));
            }
        }
    }
}
