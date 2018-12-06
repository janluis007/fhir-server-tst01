// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Messages.Delete;

namespace Microsoft.Health.Fhir.Core.Features.ControlPlane.Delete
{
    public class DeleteIdentityProviderHandler : IRequestHandler<DeleteIdentityProviderRequest, DeleteIdentityProviderResponse>
    {
        private readonly IControlPlaneRepository _repository;

        public DeleteIdentityProviderHandler(IControlPlaneRepository repository)
        {
            EnsureArg.IsNotNull(repository, nameof(repository));

            _repository = repository;
        }

        public async Task<DeleteIdentityProviderResponse> Handle(DeleteIdentityProviderRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            await _repository.DeleteIdentityProviderAsync(message.Name, cancellationToken);
            return new DeleteIdentityProviderResponse();
        }
    }
}
