// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using EnsureThat;
using Microsoft.Health.Fhir.SqlServer.Configs;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.Health.Fhir.SqlServer.Features.Schema
{
    public class SchemaUpgradeRunner
    {
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;

        public SchemaUpgradeRunner(SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration)
        {
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));

            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
        }

        public void ApplySchema(int version)
        {
            if (version != 1)
            {
                InsertSchemaVersion(version);
            }

            using (var connection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString))
            {
                connection.Open();
                var server = new Server(new ServerConnection(connection));
                server.ConnectionContext.ExecuteNonQuery(ScriptHelper.GetMigrationScript(version));
            }

            CompleteSchemaVersion(version);
        }

        private void InsertSchemaVersion(int schemaVersion)
        {
            UpsertSchemaVersion(schemaVersion, "started");
        }

        private void CompleteSchemaVersion(int schemaVersion)
        {
            UpsertSchemaVersion(schemaVersion, "complete");
        }

        private void UpsertSchemaVersion(int schemaVersion, string status)
        {
            using (var connection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString))
            {
                var upsertCommand = new SqlCommand("dbo.UpsertSchemaVersion", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                };

                upsertCommand.Parameters.AddWithValue("@version", schemaVersion);
                upsertCommand.Parameters.AddWithValue("@status", status);

                connection.Open();
                upsertCommand.ExecuteNonQuery();
            }
        }
    }
}
