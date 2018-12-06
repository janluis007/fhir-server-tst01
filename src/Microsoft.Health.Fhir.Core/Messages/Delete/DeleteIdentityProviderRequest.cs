// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;

namespace Microsoft.Health.Fhir.Core.Messages.Delete
{
    public class DeleteIdentityProviderRequest : IRequest<DeleteIdentityProviderResponse>
    {
        public DeleteIdentityProviderRequest(string name)
        {
            EnsureArg.IsNotNull(name, nameof(name));

            Name = name;
        }

        public string Name { get; }
    }
}
