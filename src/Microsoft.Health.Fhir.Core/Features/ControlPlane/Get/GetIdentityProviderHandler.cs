// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Messages.Get;

namespace Microsoft.Health.Fhir.Core.Features.ControlPlane.Get
{
    public class GetIdentityProviderHandler : IRequestHandler<GetIdentityProviderRequest, GetIdentityProviderResponse>
    {
        private readonly IControlPlaneRepository _repository;

        public GetIdentityProviderHandler(IControlPlaneRepository repository)
        {
            EnsureArg.IsNotNull(repository, nameof(repository));

            _repository = repository;
        }

        public async Task<GetIdentityProviderResponse> Handle(GetIdentityProviderRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            var response = await _repository.GetIdentityProviderAsync(message.Name, cancellationToken);

            return new GetIdentityProviderResponse(response);
        }
    }
}
