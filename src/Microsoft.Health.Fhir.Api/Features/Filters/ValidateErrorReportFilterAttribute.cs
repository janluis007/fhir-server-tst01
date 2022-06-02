// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Health.Fhir.Core.Exceptions;
using Microsoft.Health.Fhir.Core.Features;

namespace Microsoft.Health.Fhir.Api.Features.Filters
{
    /// <summary>
    /// A filter that validates that the request does not contain query parameters that are not supported
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ValidateErrorReportFilterAttribute : ActionFilterAttribute
    {
        private readonly HashSet<string> _supportedQueryParams;

        public ValidateErrorReportFilterAttribute()
        {
            _supportedQueryParams = new HashSet<string>(StringComparer.Ordinal)
            {
                KnownQueryParameterNames.Tag,
                KnownQueryParameterNames.ContinuationToken,
            };
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            IQueryCollection queryCollection = context.HttpContext.Request.Query;
            foreach (string paramName in queryCollection?.Keys)
            {
                if (IsValidBasicExportRequestParam(paramName))
                {
                    continue;
                }

                throw new RequestNotValidException(string.Format(Resources.UnsupportedParameter, paramName));
            }
        }

        private bool IsValidBasicExportRequestParam(string paramName)
        {
            return _supportedQueryParams.Contains(paramName);
        }
    }
}
