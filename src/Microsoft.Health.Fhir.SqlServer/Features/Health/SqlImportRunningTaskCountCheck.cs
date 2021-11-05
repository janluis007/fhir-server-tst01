// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Operations.Import;
using Microsoft.Health.SqlServer.Features.Client;
using Polly;
using TaskStatus = Microsoft.Health.TaskManagement.TaskStatus;

namespace Microsoft.Health.Fhir.SqlServer.Features.Health
{
    public class SqlImportRunningTaskCountCheck : IHealthCheck
    {
        private SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
        private readonly ImportTaskConfiguration _importTaskConfiguration;
        private readonly TaskHostingConfiguration _taskHostingConfiguration;
        private ILogger<SqlImportRunningTaskCountCheck> _logger;

        public SqlImportRunningTaskCountCheck(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            IOptions<OperationsConfiguration> operationsConfig,
            IOptions<TaskHostingConfiguration> taskHostingConfiguration,
            ILogger<SqlImportRunningTaskCountCheck> logger)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(operationsConfig.Value, nameof(operationsConfig));
            EnsureArg.IsNotNull(taskHostingConfiguration.Value, nameof(taskHostingConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _importTaskConfiguration = operationsConfig.Value.Import;
            _taskHostingConfiguration = taskHostingConfiguration.Value;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                int runningTaskCount = 0;
                await Policy.Handle<SqlException>()
                    .WaitAndRetryAsync(
                        retryCount: 5,
                        sleepDurationProvider: (retryCount) => TimeSpan.FromSeconds(5 * (retryCount - 1)))
                    .ExecuteAsync(async () =>
                    {
                        runningTaskCount = await GetRunningImportTaskCount(cancellationToken);
                    });

                return HealthCheckResult.Healthy($"Get {runningTaskCount} running task from sql db.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                 "Exception occured when querying sql db.",
                 exception: ex);
            }
        }

        private async Task<int> GetRunningImportTaskCount(CancellationToken cancellationToken)
        {
            short importOrchestratorTaskId = ImportOrchestratorTask.ImportOrchestratorTaskId;
            string processingTaskQueueId = string.IsNullOrEmpty(_importTaskConfiguration.ProcessingTaskQueueId) ? _taskHostingConfiguration.QueueId : _importTaskConfiguration.ProcessingTaskQueueId;

            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken, true))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
            {
                sqlCommandWrapper.CommandText = "SELECT COUNT(*) FROM [dbo].[TaskInfo] " +
                                                    "where TaskTypeId=@taskTypeId and QueueId=@queueId and Status!=@status;";
                sqlCommandWrapper.Parameters.AddWithValue("@taskTypeId", importOrchestratorTaskId);
                sqlCommandWrapper.Parameters.AddWithValue("@queueId", processingTaskQueueId);
                sqlCommandWrapper.Parameters.AddWithValue("@status", TaskStatus.Completed);
                try
                {
                    var count = await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                    return (int)count;
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"das {ex.ToString()}");
                    throw;
                }
            }
        }
    }
}
