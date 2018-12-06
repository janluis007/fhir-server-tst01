// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Messages.Create;
using Microsoft.Health.Fhir.Core.Messages.Upsert;

namespace Microsoft.Health.Fhir.Core.Features.ControlPlane.Create
{
    public class CreateIdentityProviderHandler : IRequestHandler<CreateIdentityProviderRequest, UpsertIdentityProviderResponse>
    {
        private readonly IControlPlaneRepository _repository;

        public CreateIdentityProviderHandler(IControlPlaneRepository repository)
        {
            EnsureArg.IsNotNull(repository, nameof(repository));

            _repository = repository;
        }

        public async Task<UpsertIdentityProviderResponse> Handle(CreateIdentityProviderRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            var outcome = await _repository.UpsertIdentityProviderAsync(message.IdentityProvider, null, cancellationToken);
            return new UpsertIdentityProviderResponse(outcome);
        }
    }
}
