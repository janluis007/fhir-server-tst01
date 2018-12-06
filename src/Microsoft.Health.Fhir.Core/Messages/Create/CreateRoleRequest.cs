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
    public class CreateRoleRequest : IRequest<UpsertRoleResponse>, IRequest
    {
        public CreateRoleRequest(Role role)
        {
            EnsureArg.IsNotNull(role, nameof(role));

            Role = role;
        }

        public Role Role { get; }
    }
}
