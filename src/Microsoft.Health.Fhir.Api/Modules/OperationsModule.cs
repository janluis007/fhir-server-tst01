// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Api.Features.Operations.Import;
using Microsoft.Health.Fhir.Core.Features.Operations.Export;
using Microsoft.Health.Fhir.Core.Features.Operations.Import;
using Microsoft.Health.Fhir.Operation.AzureBlob.Import;
using Microsoft.Health.Fhir.Operation.GoogleCloudPlatform;

namespace Microsoft.Health.Fhir.Api.Modules
{
    /// <summary>
    /// Registration of operations components.
    /// </summary>
    public class OperationsModule : IStartupModule
    {
        public void Load(IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.Add<ExportJobTask>()
                .Transient()
                .AsSelf();

            services.Add<IExportJobTask>(sp => sp.GetRequiredService<ExportJobTask>())
                .Transient()
                .AsSelf()
                .AsFactory();

            services.Add<ExportJobWorker>()
                .Singleton()
                .AsSelf();

            services.Add<ImportJobTaskFactory>()
                .Transient()
                .AsService<IImportJobTaskFactory>();

            services.Add<ImportJobWorker>()
                .Transient()
                .AsSelf();

            services.Add<AzureBlobImportProvider>()
                .Transient()
                .AsService<IImportProvider>();

            services.Add<BucketProvider>()
                .Transient()
                .AsService<IImportProvider>();

            services.AddHostedService<ImportJobWorkerBackgroundService>();
        }
    }
}
