// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Health.Fhir.Core.Features.Security;

namespace Microsoft.Health.Fhir.Api.Features.Audit
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class AuditLoggingFilterAttribute : ActionFilterAttribute
    {
        private readonly IClaimsExtractor _claimsExtractor;
        private readonly IAuditHelper _auditHelper;

        public AuditLoggingFilterAttribute(
            IClaimsExtractor claimsExtractor,
            IAuditHelper auditHelper)
        {
            EnsureArg.IsNotNull(claimsExtractor, nameof(claimsExtractor));
            EnsureArg.IsNotNull(auditHelper, nameof(auditHelper));

            _claimsExtractor = claimsExtractor;
            _auditHelper = auditHelper;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            context.ActionArguments.TryGetValue("failureMessage", out object failureMessage);

            _auditHelper.LogExecuting(context.HttpContext, _claimsExtractor, failureMessage?.ToString() ?? string.Empty);

            base.OnActionExecuting(context);
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            var failureMessage = string.Empty;

            if (context.Exception.Data.Contains("failureMessage"))
            {
                failureMessage = context.Exception.Data["failureMessage"].ToString();
            }

            _auditHelper.LogExecuted(context.HttpContext, _claimsExtractor, failureMessage);

            base.OnResultExecuted(context);
        }
    }
}
