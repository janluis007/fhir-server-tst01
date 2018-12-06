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
    public class UpsertRoleRequest : IRequest<UpsertRoleResponse>, IRequest
    {
        public UpsertRoleRequest(Role role, WeakETag weakETag)
        {
            EnsureArg.IsNotNull(role, nameof(role));

            Role = role;
            WeakETag = weakETag;
        }

        public Role Role { get; }

        public WeakETag WeakETag { get; }
    }
}
