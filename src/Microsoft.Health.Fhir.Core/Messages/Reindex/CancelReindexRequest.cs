// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;

namespace Microsoft.Health.Fhir.Core.Messages.Reindex
{
    public class CancelReindexRequest : IRequest<CancelReindexResponse>
    {
        public CancelReindexRequest(string jobId)
        {
            EnsureArg.IsNotNullOrWhiteSpace(jobId, nameof(jobId));

            JobId = jobId;
        }

        public string JobId { get; }
    }
}
