// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Messages.Upsert;

namespace Microsoft.Health.Fhir.Core.Features.ControlPlane.Upsert
{
    public class UpsertIdentityProviderHandler : IRequestHandler<UpsertIdentityProviderRequest, UpsertIdentityProviderResponse>
    {
        private readonly IControlPlaneRepository _repository;

        public UpsertIdentityProviderHandler(IControlPlaneRepository repository)
        {
            EnsureArg.IsNotNull(repository, nameof(repository));

            _repository = repository;
        }

        public async Task<UpsertIdentityProviderResponse> Handle(UpsertIdentityProviderRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            var outcome = await _repository.UpsertIdentityProviderAsync(message.IdentityProvider, message.WeakETag, cancellationToken);

            return new UpsertIdentityProviderResponse(outcome);
        }
    }
}
