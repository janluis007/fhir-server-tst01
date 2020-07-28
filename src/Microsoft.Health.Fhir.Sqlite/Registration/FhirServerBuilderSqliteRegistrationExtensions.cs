// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
        public static IFhirServerBuilder AddSqlite(this IFhirServerBuilder fhirServerBuilder, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(fhirServerBuilder, nameof(fhirServerBuilder));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            return fhirServerBuilder
                .AddSqlitePersistence(configuration)
                .AddSqliteSearch();
        }

        private static IFhirServerBuilder AddSqlitePersistence(this IFhirServerBuilder fhirServerBuilder, IConfiguration configuration)
        {
            IServiceCollection services = fhirServerBuilder.Services;

            services.Configure<SqliteDataStoreConfiguration>("Sqlite", sqliteConfiguration => configuration.GetSection("FhirServer:Sqlite").Bind(sqliteConfiguration));

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
