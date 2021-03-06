// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Microsoft.Health.Fhir.Core.Features.Operations.Reindex.Models;

namespace Microsoft.Health.Fhir.Core.Messages.Reindex
{
    public class CancelReindexResponse
    {
        public CancelReindexResponse(HttpStatusCode statusCode, ReindexJobWrapper job)
        {
            StatusCode = statusCode;
            Job = job;
        }

        public HttpStatusCode StatusCode { get; }

        public ReindexJobWrapper Job { get; }
    }
}
