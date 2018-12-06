// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Fhir.Core.Features.Security;

namespace Microsoft.Health.Fhir.Core.Messages.Get
{
    public class GetAllIdentityProvidersResponse
    {
        public GetAllIdentityProvidersResponse(IReadOnlyList<IdentityProvider> outcome)
        {
            EnsureArg.IsNotNull(outcome, nameof(outcome));

            Outcome = outcome;
        }

        public IReadOnlyList<IdentityProvider> Outcome { get; }
    }
}
