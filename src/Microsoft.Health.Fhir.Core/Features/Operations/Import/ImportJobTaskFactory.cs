// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;
using Microsoft.Health.Fhir.Core.Features.Persistence;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    /// <summary>
    /// Provides mechanism to create a new import job task.
    /// </summary>
    public class ImportJobTaskFactory : IImportJobTaskFactory
    {
        private readonly ImportJobConfiguration _importJobConfiguration;
        private readonly IFhirOperationDataStore _fhirOperationDataStore;
        private readonly IEnumerable<IImportProvider> _importProviders;
        private readonly IResourceWrapperFactory _resourceWrapperFactory;
        private readonly IFhirDataStore _fhirDataStore;
        private readonly FhirJsonParser _fhirJsonParser;
        private readonly ILoggerFactory _loggerFactory;

        public ImportJobTaskFactory(
            IOptions<ImportJobConfiguration> importJobConfiguration,
            IFhirOperationDataStore fhirOperationDataStore,
            IEnumerable<IImportProvider> importProviders,
            IResourceWrapperFactory resourceWrapperFactory,
            IFhirDataStore fhirDataStore,
            FhirJsonParser fhirParser,
            ILoggerFactory loggerFactory)
        {
            EnsureArg.IsNotNull(importJobConfiguration?.Value, nameof(importJobConfiguration));
            EnsureArg.IsNotNull(fhirOperationDataStore, nameof(fhirOperationDataStore));
            EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory));
            EnsureArg.IsNotNull(importProviders, nameof(importProviders));
            EnsureArg.IsNotNull(resourceWrapperFactory, nameof(resourceWrapperFactory));
            EnsureArg.IsNotNull(fhirDataStore, nameof(fhirDataStore));
            EnsureArg.IsNotNull(fhirParser, nameof(fhirParser));

            _importJobConfiguration = importJobConfiguration.Value;
            _fhirOperationDataStore = fhirOperationDataStore;
            _importProviders = importProviders;
            _resourceWrapperFactory = resourceWrapperFactory;
            _fhirDataStore = fhirDataStore;
            _fhirJsonParser = fhirParser;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public Task Create(ImportJobRecord importJobRecord, WeakETag weakETag, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(importJobRecord, nameof(importJobRecord));

            var exportJobTask = new ImportJobTask(
                importJobRecord,
                weakETag,
                _importJobConfiguration,
                _fhirOperationDataStore,
                _importProviders,
                _resourceWrapperFactory,
                _fhirDataStore,
                _fhirJsonParser,
                _loggerFactory.CreateLogger<ImportJobTask>());

            using (ExecutionContext.SuppressFlow())
            {
                return Task.Run(async () => await exportJobTask.ExecuteAsync(cancellationToken));
            }
        }
    }
}
