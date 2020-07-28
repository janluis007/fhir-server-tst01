// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Health.Fhir.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Features.Security;

namespace Microsoft.Health.Fhir.Api.Features.Audit
{
    /// <summary>
    /// Provides helper methods for auditing.
    /// </summary>
    public class AuditHelper : IAuditHelper
    {
        private readonly IFhirRequestContextAccessor _fhirRequestContextAccessor;
        private readonly IAuditLogger _auditLogger;
        private readonly IAuditHeaderReader _auditHeaderReader;

        public AuditHelper(
            IFhirRequestContextAccessor fhirRequestContextAccessor,
            IAuditLogger auditLogger,
            IAuditHeaderReader auditHeaderReader)
        {
            EnsureArg.IsNotNull(fhirRequestContextAccessor, nameof(fhirRequestContextAccessor));
            EnsureArg.IsNotNull(auditLogger, nameof(auditLogger));
            EnsureArg.IsNotNull(auditHeaderReader, nameof(auditHeaderReader));

            _fhirRequestContextAccessor = fhirRequestContextAccessor;
            _auditLogger = auditLogger;
            _auditHeaderReader = auditHeaderReader;
        }

        /// <inheritdoc />
        public void LogExecuting(HttpContext httpContext, IClaimsExtractor claimsExtractor)
        {
            EnsureArg.IsNotNull(claimsExtractor, nameof(claimsExtractor));
            EnsureArg.IsNotNull(httpContext, nameof(httpContext));

            Log(AuditAction.Executing, statusCode: null, httpContext, claimsExtractor);
        }

        /// <inheritdoc />
        public void LogExecuted(HttpContext httpContext, IClaimsExtractor claimsExtractor)
        {
            EnsureArg.IsNotNull(claimsExtractor, nameof(claimsExtractor));
            EnsureArg.IsNotNull(httpContext, nameof(httpContext));

            Log(AuditAction.Executed, (HttpStatusCode)httpContext.Response.StatusCode, httpContext, claimsExtractor);
        }

        private void Log(AuditAction auditAction, HttpStatusCode? statusCode, HttpContext httpContext, IClaimsExtractor claimsExtractor)
        {
            IFhirRequestContext fhirRequestContext = _fhirRequestContextAccessor.FhirRequestContext;

            if (fhirRequestContext == null)
            {
                InitFhirRequestContext(fhirRequestContext, httpContext);
                fhirRequestContext = _fhirRequestContextAccessor.FhirRequestContext;
            }

            string auditEventType = fhirRequestContext.AuditEventType;

            // Audit the call if an audit event type is associated with the action.
            if (!string.IsNullOrEmpty(auditEventType))
            {
                _auditLogger.LogAudit(
                    auditAction,
                    operation: auditEventType,
                    resourceType: fhirRequestContext.ResourceType,
                    requestUri: fhirRequestContext.Uri,
                    statusCode: statusCode,
                    correlationId: fhirRequestContext.CorrelationId,
                    callerIpAddress: httpContext.Connection?.RemoteIpAddress?.ToString(),
                    callerClaims: claimsExtractor.Extract(),
                    customHeaders: _auditHeaderReader.Read(httpContext));
            }
        }

        private void InitFhirRequestContext(IFhirRequestContext fhirRequestContext, HttpContext context)
        {
            const string RequestIdHeaderName = "X-Request-Id";
            var request = context.Request;

            string baseUriInString = UriHelper.BuildAbsolute(
                request.Scheme,
                request.Host,
                request.PathBase);

            string uriInString = UriHelper.BuildAbsolute(
                request.Scheme,
                request.Host,
                request.PathBase,
                request.Path,
                request.QueryString);

            string correlationId = "FakeCorrelationId";

            fhirRequestContext = new FhirRequestContext(
                method: request.Method,
                uriString: uriInString,
                baseUriString: baseUriInString,
                correlationId: correlationId,
                requestHeaders: request.Headers,
                responseHeaders: request.Headers);

            try
            {
                context.Response.Headers[RequestIdHeaderName] = correlationId;
            }
            catch
            {
                // swallow exception
            }

            _fhirRequestContextAccessor.FhirRequestContext = fhirRequestContext;
        }
    }
}
