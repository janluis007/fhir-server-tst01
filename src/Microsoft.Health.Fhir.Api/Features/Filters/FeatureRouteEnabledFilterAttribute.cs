// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Fhir.Api.Features.Filters
{
    /// <summary>
    /// A filter that when added to a controller or action will return a 404 for the route if the specified configuration key is not True.
    /// </summary>
    /// <example>[FeatureRouteEnabledFilterAttribute(ConfigurationPath = "FhirServer:Features:SupportsValidate")]</example>
    public class FeatureRouteEnabledFilterAttribute : ActionFilterAttribute
    {
        public string ConfigurationPath { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            var configuration = (IConfiguration)context.HttpContext.RequestServices.GetService(typeof(IConfiguration));

            var value = configuration[ConfigurationPath];

            if (string.IsNullOrEmpty(value) || !bool.TryParse(value, out var result) || !result)
            {
                context.Result = new NotFoundResult();
            }
        }
    }
}
