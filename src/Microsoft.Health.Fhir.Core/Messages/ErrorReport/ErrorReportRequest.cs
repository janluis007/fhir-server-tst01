// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Fhir.Core.Messages.ErrorReport
{
    public class ErrorReportRequest : IRequest<ErrorReportResponse>
    {
        public ErrorReportRequest(string tag, string continuationToken = null)
        {
            Tag = tag;
            ContinuationToken = continuationToken;
        }

        public string Tag { get; }

        public string ContinuationToken { get; }
    }
}
