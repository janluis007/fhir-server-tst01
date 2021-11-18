// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer;

namespace Microsoft.Health.Fhir.SqlServer.Features.Search
{
    /// <summary>
    /// Default Sql Connection Factory is responsible to handle Sql connections that can be made purely based on connection string.
    /// Connection string containing user name and password, or integrated auth are perfect examples for this.
    /// </summary>
    public class DelayedSqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly ISqlConnectionStringProvider _sqlConnectionStringProvider;

        public DelayedSqlConnectionFactory(ISqlConnectionStringProvider sqlConnectionStringProvider)
        {
            EnsureArg.IsNotNull(sqlConnectionStringProvider, nameof(sqlConnectionStringProvider));

            _sqlConnectionStringProvider = sqlConnectionStringProvider;
        }

        /// <inheritdoc />
        public async Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog = null, CancellationToken cancellationToken = default)
        {
            SqlConnection sqlConnection;
            string sqlConnectionString = await _sqlConnectionStringProvider.GetSqlConnectionString(cancellationToken);
            if (string.IsNullOrEmpty(sqlConnectionString))
            {
                throw new InvalidOperationException("The SQL connection string cannot be null or empty.");
            }

            if (initialCatalog == null)
            {
                SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder(sqlConnectionString);
                connectionBuilder.MaxPoolSize = 3;
                sqlConnection = new SqlConnection(connectionBuilder.ToString());
            }
            else
            {
                SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder(sqlConnectionString) { InitialCatalog = initialCatalog };
                connectionBuilder.MaxPoolSize = 3;
                sqlConnection = new SqlConnection(connectionBuilder.ToString());
            }

            return sqlConnection;
        }
    }
}
