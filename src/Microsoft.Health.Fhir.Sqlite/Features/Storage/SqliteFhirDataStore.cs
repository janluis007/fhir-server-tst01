// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Core.Features.Conformance;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.Sqlite.Configs;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Sqlite.Features.Storage
{
    /// <summary>
    /// A SQLite-backed <see cref="IFhirDataStore"/>.
    /// </summary>
    public class SqliteFhirDataStore : IFhirDataStore, IProvideCapability
    {
        private readonly IModelInfoProvider _modelInfoProvider;
        private readonly ILogger<SqliteFhirDataStore> _logger;
        private readonly string _connectionString;

        public SqliteFhirDataStore(
            SqliteDataStoreConfiguration configuration,
            IModelInfoProvider modelInfoProvider,
            ILogger<SqliteFhirDataStore> logger)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(modelInfoProvider, nameof(modelInfoProvider));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _modelInfoProvider = modelInfoProvider;
            _logger = logger;

            _connectionString = configuration.ConnectionString;
        }

        public Task<UpsertOutcome> UpsertAsync(ResourceWrapper resource, WeakETag weakETag, bool allowCreate, bool keepHistory, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ResourceWrapper> GetAsync(ResourceKey key, CancellationToken cancellationToken)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string id = "test";

                var command = connection.CreateCommand();
                command.CommandText =
                    @"
                        SELECT name
                        FROM user
                        WHERE id = $id
                    ";
                command.Parameters.AddWithValue("$id", id);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                    }
                }
            }

            return (Task<ResourceWrapper>)Task.CompletedTask;
        }

        public Task HardDeleteAsync(ResourceKey key, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Build(ICapabilityStatementBuilder builder)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));

            // TODO: Update this to accurately reflect what is supported.
            builder.AddDefaultResourceInteractions()
                   .AddDefaultSearchParameters()
                   .AddDefaultRestSearchParams();
        }
    }
}
