// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Extensions.DependencyInjection;
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
        private readonly Func<IScoped<IFhirOperationDataStore>> _fhirOperationDataStoreFactory;
        private readonly IEnumerable<IImportProvider> _importProviders;
        private readonly IResourceWrapperFactory _resourceWrapperFactory;
        private readonly Func<IScoped<IFhirDataStore>> _fhirDataStoreFactory;
        private readonly ResourceDeserializer _resourceDeserializer;
        private readonly ILoggerFactory _loggerFactory;

        public ImportJobTaskFactory(
            IOptions<ImportJobConfiguration> importJobConfiguration,
            Func<IScoped<IFhirOperationDataStore>> fhirOperationDataStoreFactory,
            IEnumerable<IImportProvider> importProviders,
            IResourceWrapperFactory resourceWrapperFactory,
            Func<IScoped<IFhirDataStore>> fhirDataStoreFactory,
            ResourceDeserializer resourceDeserializer,
            ILoggerFactory loggerFactory)
        {
            EnsureArg.IsNotNull(importJobConfiguration?.Value, nameof(importJobConfiguration));
            EnsureArg.IsNotNull(fhirOperationDataStoreFactory, nameof(fhirOperationDataStoreFactory));
            EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory));
            EnsureArg.IsNotNull(importProviders, nameof(importProviders));
            EnsureArg.IsNotNull(resourceWrapperFactory, nameof(resourceWrapperFactory));
            EnsureArg.IsNotNull(fhirDataStoreFactory, nameof(fhirDataStoreFactory));
            EnsureArg.IsNotNull(resourceDeserializer, nameof(resourceDeserializer));

            _importJobConfiguration = importJobConfiguration.Value;
            _fhirOperationDataStoreFactory = fhirOperationDataStoreFactory;
            _importProviders = importProviders;
            _resourceWrapperFactory = resourceWrapperFactory;
            _fhirDataStoreFactory = fhirDataStoreFactory;
            _resourceDeserializer = resourceDeserializer;
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
                _fhirOperationDataStoreFactory,
                _importProviders,
                _resourceWrapperFactory,
                _fhirDataStoreFactory,
                _resourceDeserializer,
                _loggerFactory.CreateLogger<ImportJobTask>());

            using (ExecutionContext.SuppressFlow())
            {
                return Task.Run(async () => await exportJobTask.ExecuteAsync(cancellationToken));
            }
        }
    }
}
