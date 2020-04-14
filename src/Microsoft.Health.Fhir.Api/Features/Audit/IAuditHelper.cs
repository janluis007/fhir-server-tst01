// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Health.Fhir.Core.Features.Security;

namespace Microsoft.Health.Fhir.Api.Features.Audit
{
    /// <summary>
    /// Provides helper methods for auditing.
    /// </summary>
    public interface IAuditHelper
    {
        /// <summary>
        /// Logs an executing audit entry for the current operation.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <param name="claimsExtractor">The extractor used to extract claims.</param>
        /// /// <param name="failuremessage">The failure message providing more context to the http status code for a failed request.</param>
        void LogExecuting(HttpContext httpContext, IClaimsExtractor claimsExtractor, string failuremessage);

        /// <summary>
        /// Logs an executed audit entry for the current operation.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <param name="claimsExtractor">The extractor used to extract claims.</param>
        /// <param name="failuremessage">The failure message providing more context to the http status code for a failed request.</param>
        void LogExecuted(HttpContext httpContext, IClaimsExtractor claimsExtractor, string failuremessage);
    }
}
