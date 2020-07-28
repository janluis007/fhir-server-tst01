// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Registration;
using Microsoft.Health.Fhir.Sqlite.Configs;
using Microsoft.Health.Fhir.Sqlite.Features.Search;
using Microsoft.Health.Fhir.Sqlite.Features.Storage;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FhirServerBuilderSqliteRegistrationExtensions
    {
        public static IFhirServerBuilder AddSqlite(this IFhirServerBuilder fhirServerBuilder, Action<SqliteDataStoreConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(fhirServerBuilder, nameof(fhirServerBuilder));

            return fhirServerBuilder
                .AddSqlitePersistence(configureAction)
                .AddSqliteSearch();
        }

        private static IFhirServerBuilder AddSqlitePersistence(this IFhirServerBuilder fhirServerBuilder, Action<SqliteDataStoreConfiguration> configureAction = null)
        {
            IServiceCollection services = fhirServerBuilder.Services;

            services.Add(provider =>
                {
                    var config = new SqliteDataStoreConfiguration();
                    provider.GetService<IConfiguration>().GetSection("Sqlite").Bind(config);
                    configureAction?.Invoke(config);

                    return config;
                })
                .Singleton()
                .AsSelf();

            services.Add<SqliteFhirDataStore>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            fhirServerBuilder.Services.AddHealthChecks();

            return fhirServerBuilder;
        }

        private static IFhirServerBuilder AddSqliteSearch(this IFhirServerBuilder fhirServerBuilder)
        {
            fhirServerBuilder.Services.Add<SqliteSearchService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return fhirServerBuilder;
        }
    }
}
