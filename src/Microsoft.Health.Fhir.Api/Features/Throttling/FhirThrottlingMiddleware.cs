// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Fhir.Api.Features.Throttling
{
    public class FhirThrottlingMiddleware : IpRateLimitMiddleware
    {
        public FhirThrottlingMiddleware(
            RequestDelegate next,
            IOptions<IpRateLimitOptions> options,
            IRateLimitCounterStore counterStore,
            IIpPolicyStore policyStore,
            IRateLimitConfiguration config,
            ILogger<FhirThrottlingMiddleware> logger)
                : base(next, options, counterStore, policyStore, config, logger)
        {
        }

        public override Task ReturnQuotaExceededResponse(
            HttpContext httpContext,
            RateLimitRule rule,
            string retryAfter)
        {
            return httpContext.Response.WriteAsync("{ \"resourceType\":\"OperationOutcome\",\"issue\":[{\"severity\":\"error\",\"code\":\"ratelimit\",\"diagnostics\":\"Request limit exceeded. Try again later.\"}]}");
        }
    }
}
