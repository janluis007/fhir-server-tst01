// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using FluentValidation.Results;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Health.Fhir.Api.Features.Routing;
using Microsoft.Health.Fhir.Core.Features.Validation;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Api.Features.Filters
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class ValidateResourceTypeFilterAttribute : ParameterCompatibleFilter
    {
        public ValidateResourceTypeFilterAttribute(bool allowParametersResource = false)
            : base(allowParametersResource)
        {
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            if (context.RouteData.Values.TryGetValue(KnownActionParameterNames.ResourceType, out var actionModelType) &&
                context.ActionArguments.TryGetValue(KnownActionParameterNames.Resource, out var parsedModel))
            {
                var resource = ParseResource((ResourceElement)parsedModel);
                ValidateType(resource, (string)actionModelType);
            }
        }

        private static void ValidateType(ResourceElement resource, string expectedType)
        {
            if (!string.Equals(expectedType, resource.InstanceType, StringComparison.OrdinalIgnoreCase))
            {
                throw new ResourceNotValidException(new List<ValidationFailure>
                    {
                        new ValidationFailure(nameof(Base.TypeName), Api.Resources.ResourceTypeMismatch),
                    });
            }
        }
    }
}
