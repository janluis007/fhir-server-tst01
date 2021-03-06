// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Api.Features.Audit;

namespace Microsoft.Health.Fhir.Api.Controllers
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class AadSmartOnFhirProxyAuditLoggingFilterAttribute : AuditLoggingFilterAttribute
    {
        public AadSmartOnFhirProxyAuditLoggingFilterAttribute(
            AadSmartOnFhirClaimsExtractor claimsExtractor,
            IAuditHelper auditHelper)
            : base(claimsExtractor, auditHelper)
        {
        }
    }
}
