// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Security;
using Microsoft.Health.Fhir.Core.Messages.Upsert;

namespace Microsoft.Health.Fhir.Core.Messages.Create
{
    public class CreateIdentityProviderRequest : IRequest<UpsertIdentityProviderResponse>, IRequest
    {
        public CreateIdentityProviderRequest(IdentityProvider identityProvider)
        {
            EnsureArg.IsNotNull(identityProvider, nameof(identityProvider));

            IdentityProvider = identityProvider;
        }

        public IdentityProvider IdentityProvider { get; }
    }
}
