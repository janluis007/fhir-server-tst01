// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Fhir.Api.Features.ActionResults;
using Microsoft.Health.Fhir.Api.Features.Filters;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Messages.Operation;
using Microsoft.Health.Fhir.ValueSets;

namespace Microsoft.Health.Fhir.Api.Controllers
{
    [ServiceFilter(typeof(AuditLoggingFilterAttribute))]
    [ServiceFilter(typeof(OperationOutcomeExceptionFilterAttribute))]
    [FeatureRouteEnabledFilterAttribute(ConfigurationPath = "CosmosDb:SortConfigOperationEnabled")]
    public class SortController : Controller
    {
        private readonly IMediator _mediator;

        public SortController(IMediator mediator)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));

            _mediator = mediator;
        }

        [HttpPost]
        [Route("$settings-sort")]
        [AuditEventType(AuditEventSubType.SortSettings)]
        public async Task<IActionResult> UpdateSortSettings([FromBody] Resource resource)
        {
            SortSettingsResponse response = await _mediator.Send(new UpdateSortSettingsRequest(resource.ToResourceElement()));

            return CreateResponse(response);
        }

        [HttpGet]
        [Route("$settings-sort")]
        [AuditEventType(AuditEventSubType.SortSettings)]
        public async Task<IActionResult> GetSortSettings(Uri uri)
        {
            SortSettingsResponse response = await _mediator.Send(new GetSortSettingsRequest(uri));

            return CreateResponse(response);
        }

        private static IActionResult CreateResponse(SortSettingsResponse response)
        {
            var parameter = new Parameters();

            parameter.Add("uri", new FhirUri(response.Uri));
            parameter.Add("status", new FhirString(response.Status.ToString()));

            return FhirResult.Create(parameter.ToResourceElement());
        }
    }
}
