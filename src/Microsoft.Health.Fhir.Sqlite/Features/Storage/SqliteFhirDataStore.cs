// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Features.Conformance;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Sqlite.Configs;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Sqlite.Features.Storage
{
    /// <summary>
    /// A SQLite-backed <see cref="IFhirDataStore"/>.
    /// </summary>
    public class SqliteFhirDataStore : IFhirDataStore, IProvideCapability, IStartable
    {
        private readonly string _connectionString;
        private readonly string _databaseFileName;

        public SqliteFhirDataStore(
            SqliteDataStoreConfiguration configuration)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            _connectionString = configuration.ConnectionString;
            _databaseFileName = configuration.DatabaseFileName;
        }

        // TODO: Add check if resource exists - right now, this only creates new resources.
        public Task<UpsertOutcome> UpsertAsync(ResourceWrapper resource, WeakETag weakETag, bool allowCreate, bool keepHistory, CancellationToken cancellationToken)
        {
            var connection = new SQLiteConnection(_connectionString);
            string sql = @"
                INSERT INTO Resource
                    (ResourceTypeName, ResourceId, Version, RawResource)
                VALUES
                    (@resourceTypeName, @resourceId, 1, @rawResource)";

            try
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@resourceTypeName", resource.ResourceTypeName);
                    command.Parameters.AddWithValue("@resourceId", resource.ResourceId);
                    command.Parameters.AddWithValue("@rawResource", resource.RawResource.Data);

                    command.ExecuteNonQuery();
                    resource.Version = "1";
                }
            }
            finally
            {
                connection.Close();
            }

            return Task.FromResult(new UpsertOutcome(resource, SaveOutcomeType.Created));
        }

        public Task<ResourceWrapper> GetAsync(ResourceKey key, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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

        public void Start()
        {
            SQLiteConnection.CreateFile(_databaseFileName);

            var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // TODO: Update RawResource to blob?
            // TODO: Replace resource type name with ID
            string sql = @"CREATE TABLE IF NOT EXISTS Resource (
                ResourceTypeName varchar(64) NOT NULL,
                ResourceId varchar(64) NOT NULL,
                Version int NOT NULL,
                RawResource varchar(65536))";

            // RawResource varchar(256))";

            var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();

            connection.Close();
        }
    }
}
