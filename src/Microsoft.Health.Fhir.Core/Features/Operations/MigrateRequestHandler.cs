// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Messages.Migrate;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Operations
{
    public class MigrateRequestHandler : IRequestHandler<MigrateRequest, MigrateResponse>
    {
        private IFhirDataMigrationHandler _migrateHandler;
        private IMigrateDataExporter _dataExporter;

        public MigrateRequestHandler(
            IFhirDataMigrationHandler migrateHandler,
            IMigrateDataExporter dataExporter)
        {
            _migrateHandler = migrateHandler;
            _dataExporter = dataExporter;
        }

        public async Task<MigrateResponse> Handle(MigrateRequest request, CancellationToken cancellationToken)
        {
            return request.RequestType switch
            {
                MigrateRequestType.Migrate => await DoMigration(request.MigrationCount),
                MigrateRequestType.ExportOnly => await DoExportOnly(request.MigrationCount),
                _ => throw new NotImplementedException(),
            };
        }

        private async Task<MigrateResponse> DoExportOnly(int resourceLimit)
        {
            var stopWatch = Stopwatch.StartNew();
            int count = 0;
            await foreach (var data in _dataExporter.Export())
            {
                Console.WriteLine($"Resources: {data.Count()}");
                count += data.Count;
                if (count >= resourceLimit)
                {
                    break;
                }
            }

            stopWatch.Stop();

            var exportResult = new ExportRateResult
            {
                Count = $"{count}",
                Time = $"{stopWatch.ElapsedMilliseconds}",
            };

            return new MigrateResponse
            {
                Succeed = true,
                Message = JsonConvert.SerializeObject(exportResult),
            };
        }

        private async Task<MigrateResponse> DoMigration(int resourceLimit)
        {
            var stopWatch = Stopwatch.StartNew();
            int count = 0;

            var tasks = new List<Task<string>>();
            await foreach (var data in _dataExporter.Export())
            {
                Console.WriteLine($"Resources: {data.Count()}");
                tasks.Add(Task.Run(() => Migrate(data)));
                count += data.Count;
                if (count >= resourceLimit)
                {
                    break;
                }
            }

            var results = await Task.WhenAll(tasks);
            stopWatch.Stop();

            if (results.FirstOrDefault(x => !string.IsNullOrEmpty(x)) != null)
            {
                return new MigrateResponse
                {
                    Succeed = true,
                    Message = results.FirstOrDefault(x => !string.IsNullOrEmpty(x)),
                };
            }

            var exportResult = new ExportRateResult
            {
                Count = $"{count}",
                Time = $"{stopWatch.ElapsedMilliseconds}",
            };

            return new MigrateResponse
            {
                Succeed = true,
                Message = JsonConvert.SerializeObject(exportResult),
            };
        }

        private async Task<string> Migrate(List<ResourceWrapper> data)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now}: Working...");
                await _migrateHandler.Process(data);
                return null;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        internal class ExportRateResult
        {
            public string Count { get; set; }

            public string Time { get; set; }
        }
    }
}
