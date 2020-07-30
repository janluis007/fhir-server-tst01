// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Extensions.DependencyInjection;
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
    public class SqliteFhirDataStore : IFhirDataStore, IProvideCapability, IStartable
    {
        private readonly IModelInfoProvider _modelInfoProvider;
        private readonly ILogger<SqliteFhirDataStore> _logger;
        private readonly string _connectionString;
        private readonly string _databaseFileName;

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
            _databaseFileName = configuration.DatabaseFileName;
        }

        // TODO: Add check if resource exists.
        public Task<UpsertOutcome> UpsertAsync(ResourceWrapper resource, WeakETag weakETag, bool allowCreate, bool keepHistory, CancellationToken cancellationToken)
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // TODO: parameterize, add resource values from client
            string sql = @"INSERT INTO Resource (ResourceTypeId, ResourceId, RawResource) VALUES (1, 'Patient', 'Test')";

            var command = new SQLiteCommand(sql, connection);
            var newVersion = command.ExecuteNonQuery();

            connection.Close();

            // TODO: Add version info to resource table
            resource.Version = newVersion.ToString();

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
            string sql = "CREATE TABLE IF NOT EXISTS Resource (ResourceTypeId int, ResourceId varchar(64), RawResource varchar(256))";

            var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();

            connection.Close();
        }
    }
}
