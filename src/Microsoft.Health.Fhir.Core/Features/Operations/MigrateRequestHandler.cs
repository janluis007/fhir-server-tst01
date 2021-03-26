// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
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
            await foreach (var data in _dataExporter.Export())
            {
                try
                {
                    await _migrateHandler.Process(data);
                }
                catch (Exception ex)
                {
                    return new MigrateResponse
                    {
                        Succeed = false,
                        Message = ex.ToString(),
                    };
                }

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

        internal class ExportRateResult
        {
            public string Count { get; set; }

            public string Time { get; set; }
        }
    }
}
