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
    public class GetRoleHandler : IRequestHandler<GetRoleRequest, GetRoleResponse>
    {
        private readonly IControlPlaneRepository _repository;

        public GetRoleHandler(IControlPlaneRepository repository)
        {
            EnsureArg.IsNotNull(repository, nameof(repository));

            _repository = repository;
        }

        public async Task<GetRoleResponse> Handle(GetRoleRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            var response = await _repository.GetRoleAsync(message.Name, cancellationToken);

            return new GetRoleResponse(response);
        }
    }
}
