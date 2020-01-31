// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Api.Configs;
using Microsoft.Health.Fhir.Api.Features.ActionResults;
using Microsoft.Health.Fhir.Api.Features.Audit;
using Microsoft.Health.Fhir.Api.Features.Filters;
using Microsoft.Health.Fhir.Api.Features.Routing;
using Microsoft.Health.Fhir.Api.Features.Security;
using Microsoft.Health.Fhir.Core.Exceptions;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Features;
using Microsoft.Health.Fhir.Core.Features.Operations;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Messages.Operation;
using Microsoft.Health.Fhir.ValueSets;

namespace Microsoft.Health.Fhir.Api.Controllers
{
    [ServiceFilter(typeof(AuditLoggingFilterAttribute))]
    [ServiceFilter(typeof(OperationOutcomeExceptionFilterAttribute))]
    [ServiceFilter(typeof(ValidateContentTypeFilterAttribute))]
    [ValidationModeFilter]
    [ValidateModelState]
    [Authorize(PolicyNames.FhirPolicy)]
    public class ValidateController : Controller
    {
        private readonly IMediator _mediator;
        private readonly FeatureConfiguration _features;
        private readonly FhirJsonParser _parser;

        public ValidateController(IMediator mediator, IOptions<FeatureConfiguration> features, FhirJsonParser parser)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(features, nameof(features));
            EnsureArg.IsNotNull(features.Value, nameof(features));
            EnsureArg.IsNotNull(parser, nameof(parser));

            _mediator = mediator;
            _features = features.Value;
            _parser = parser;
        }

        [HttpPost]
        [Route(KnownRoutes.ValidateResourceType)]
        [AuditEventType(AuditEventSubType.Read)]
        [Authorize(PolicyNames.ReadPolicy)]
        public async Task<IActionResult> Validate([FromBody] Resource resource, [FromQuery(Name = KnownQueryParameterNames.Profile)] string profile, [FromQuery(Name = KnownQueryParameterNames.Mode)] string mode, string typeParameter)
        {
            return await RunValidationAsync(resource, profile, mode, typeParameter);
        }

        [HttpPost]
        [Route(KnownRoutes.ValidateResourceTypeById)]
        [AuditEventType(AuditEventSubType.Read)]
        [Authorize(PolicyNames.ReadPolicy)]
        public async Task<IActionResult> ValidateById([FromBody] Resource resource, [FromQuery(Name = KnownQueryParameterNames.Profile)] string profile, [FromQuery(Name = KnownQueryParameterNames.Mode)] string mode, string typeParameter, string idParameter)
        {
            return await RunValidationAsync(resource, profile, mode, typeParameter, idParameter, true);
        }

        private async Task<IActionResult> RunValidationAsync(Resource resource, string profile, string mode, string typeParameter, string idParameter = null, bool idMode = false)
        {
            if (!_features.SupportsValidate)
            {
                throw new OperationNotImplementedException(Resources.ValidationNotSupported);
            }

            if (resource.ResourceType == ResourceType.Parameters)
            {
                resource = ParseParameters((Parameters)resource, ref profile, ref mode);
            }

            // This is the same as the filter that is applied in the ValidationModeFilter.
            // It is needed here to cover the case of the mode being passed as part of a Parameters resource.
            // It is needed as a filter attribute so that it can perform the filter before the ValidateModelState filter returns an error if the user passed an invalid resource.
            // This is needed because if a user requests a delete validation it doesn't matter what resource they pass, so the delete validation should run regardless of if the resource is valid.
            ValidationModeFilterAttribute.ParseMode(mode, idMode);

            ValidateResourceTypeFilterAttribute.ValidateType(resource, typeParameter);

            if (idMode)
            {
                ValidateResourceIdFilterAttribute.ValidateId(resource, idParameter);
            }

            OperationOutcome profileOutcome = null;
            if (profile != null)
            {
                try
                {
                    var webRequest = WebRequest.Create(new Uri(profile));
                    var profileResponse = (HttpWebResponse)webRequest.GetResponse();
                    StructureDefinition profileResource = null;

                    using (var streamReader = new StreamReader(profileResponse.GetResponseStream()))
                    {
                        string jsonText = streamReader.ReadToEnd();
                        profileResource = _parser.Parse<StructureDefinition>(jsonText);
                    }

                    var validator = new Hl7.Fhir.Validation.Validator();
                    profileOutcome = validator.Validate(resource.ToResourceElement().Instance, profileResource);
                }
                catch (WebException ex)
                {
                    throw new ResourceNotFoundException(ex.Message);
                }
            }

            var response = await _mediator.Send<ValidateOperationResponse>(new ValidateOperationRequest(resource.ToResourceElement()));

            var issues = response.Issues.Select(x => x.ToPoco()).ToList();
            if (profileOutcome != null)
            {
                issues.AddRange(profileOutcome.Issue);
            }

            return FhirResult.Create(new OperationOutcome
            {
                Issue = issues,
            }.ToResourceElement());
        }

        private static Resource ParseParameters(Parameters resource, ref string profile, ref string mode)
        {
            var paramMode = resource.Parameter.Find(param => param.Name.Equals("mode", System.StringComparison.OrdinalIgnoreCase));
            if (paramMode != null && mode != null)
            {
                throw new BadRequestException(Resources.MultipleModesProvided);
            }
            else if (paramMode != null && mode == null)
            {
                mode = paramMode.Value.ToString();
            }

            var paramProfile = resource.Parameter.Find(param => param.Name.Equals("profile", System.StringComparison.OrdinalIgnoreCase));
            if (paramProfile != null && profile != null)
            {
                throw new BadRequestException(Resources.MultipleProfilesProvided);
            }
            else if (paramProfile != null && profile == null)
            {
                profile = paramProfile.Value.ToString();
            }

            return resource.Parameter.Find(param => param.Name.Equals("resource", System.StringComparison.OrdinalIgnoreCase)).Resource;
        }
    }
}
