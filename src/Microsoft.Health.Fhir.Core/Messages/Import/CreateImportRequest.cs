// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;

namespace Microsoft.Health.Fhir.Core.Messages.Import
{
    public class CreateImportRequest : IRequest<CreateImportResponse>
    {
        public CreateImportRequest(Uri requestUri, ImportRequest importRequest)
        {
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));
            EnsureArg.IsNotNull(importRequest, nameof(importRequest));

            RequestUri = requestUri;
            ImportRequest = importRequest;
        }

        public Uri RequestUri { get; }

        public ImportRequest ImportRequest { get; }
    }
}
