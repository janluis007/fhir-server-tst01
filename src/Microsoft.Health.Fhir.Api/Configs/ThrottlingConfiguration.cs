// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using AspNetCoreRateLimit;

namespace Microsoft.Health.Fhir.Api.Configs
{
    public class ThrottlingConfiguration
    {
        public IpRateLimitOptions IpRateLimiting { get; } = new IpRateLimitOptions();

        public IpRateLimitPolicies IpRateLimitPolicies { get; } = new IpRateLimitPolicies();
    }
}
