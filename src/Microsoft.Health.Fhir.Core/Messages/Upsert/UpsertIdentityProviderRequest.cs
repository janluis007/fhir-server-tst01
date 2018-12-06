// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Security;

namespace Microsoft.Health.Fhir.Core.Messages.Upsert
{
    public class UpsertIdentityProviderRequest : IRequest<UpsertIdentityProviderResponse>, IRequest
    {
        public UpsertIdentityProviderRequest(IdentityProvider identityProvider, WeakETag weakETag)
        {
            EnsureArg.IsNotNull(identityProvider, nameof(identityProvider));

            IdentityProvider = identityProvider;
            WeakETag = weakETag;
        }

        public IdentityProvider IdentityProvider { get; }

        public WeakETag WeakETag { get; }
    }
}
