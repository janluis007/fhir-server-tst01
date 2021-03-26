// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Fhir.Api.Features.Filters;
using Microsoft.Health.Fhir.Api.Features.Routing;
using Microsoft.Health.Fhir.Core.Features;
using Microsoft.Health.Fhir.Core.Messages.Migrate;
using Microsoft.Health.Fhir.ValueSets;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Api.Controllers
{
    [ServiceFilter(typeof(AuditLoggingFilterAttribute))]
    [ServiceFilter(typeof(OperationOutcomeExceptionFilterAttribute))]
    public class MigrateController
    {
        private IMediator _mediator;

        public MigrateController(
            IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Route(KnownRoutes.Migrate)]
        [AuditEventType(AuditEventSubType.ConvertData)]
        public async Task<IActionResult> Migrate(
            [FromQuery(Name = KnownQueryParameterNames.MigrationCount)] int migrationCount)
        {
            if (migrationCount <= 0)
            {
                return new ContentResult
                {
                    Content = "Resource count is invalid.",
                    StatusCode = 400,
                };
            }

            var request = new MigrateRequest("123", MigrateRequestType.Migrate, migrationCount);
            var response = await _mediator.Send(request, CancellationToken.None);

            return new ContentResult
            {
                Content = JsonConvert.SerializeObject(response),
                StatusCode = 200,
            };
        }

        [HttpGet]
        [Route(KnownRoutes.ExportOnly)]
        [AuditEventType(AuditEventSubType.ConvertData)]
        public async Task<IActionResult> ExportTest(
            [FromQuery(Name = KnownQueryParameterNames.MigrationCount)] int migrationCount)
        {
            var request = new MigrateRequest("123", MigrateRequestType.ExportOnly, migrationCount);
            var response = await _mediator.Send(request, CancellationToken.None);

            return new ContentResult
            {
                Content = JsonConvert.SerializeObject(response),
                StatusCode = 200,
            };
        }
    }
}
