// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.Headers;
using Microsoft.Health.Fhir.Api.Configs;
using Microsoft.Health.Fhir.Api.Features.ApiNotifications;
using Microsoft.Health.Fhir.Api.Features.Audit;
using Microsoft.Health.Fhir.Api.Features.Context;
using Microsoft.Health.Fhir.Api.Features.Exceptions;
using Microsoft.Health.Fhir.Api.Features.Routing;
using Microsoft.Health.Fhir.Api.Features.Throttling;
using Microsoft.Health.Fhir.Core.Features.Cors;

namespace Microsoft.Health.Fhir.Api.Registration
{
    public static class FhirServerStartupFilter
    {
        public static IApplicationBuilder Configure(IApplicationBuilder app)
        {
            IWebHostEnvironment env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            // This middleware will add delegates to the OnStarting method of httpContext.Response for setting headers.
            app.UseBaseHeaders();

            app.UseCors(Constants.DefaultCorsPolicy);

            // This middleware should be registered at the beginning since it generates correlation id among other things,
            // which will be used in other middlewares.
            app.UseFhirRequestContext();

            // This middleware will capture issues within other middleware that prevent the ExceptionHandler from completing.
            // This should be the first middleware added because they execute in order.
            app.UseBaseException();

            // This middleware will capture any unhandled exceptions and attempt to return an operation outcome using the customError page
            app.UseExceptionHandler(KnownRoutes.CustomError);

            // This middleware will capture any handled error with the status code between 400 and 599 that hasn't had a body or content-type set. (i.e. 404 on unknown routes)
            app.UseStatusCodePagesWithReExecute(KnownRoutes.CustomError, "?statusCode={0}");

            // The audit module needs to come after the exception handler because we need to catch the response before it gets converted to custom error.
            app.UseAudit();
            app.UseApiNotifications();

            app.UseFhirRequestContextAuthentication();

            var throttlingConfig = app.ApplicationServices.GetService<IOptions<ThrottlingConfiguration>>();

            if (throttlingConfig?.Value?.Enabled == true)
            {
                // Throttling needs to come after Audit and ApiNotifications so we can audit it and track it for API metrics.
                // It should also be after authentication
                app.UseThrottling();
            }

            return app;
        }
    }
}
