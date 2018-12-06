// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.Core.Features.Security;

namespace Microsoft.Health.Fhir.Core.Features.Persistence
{
    public interface IControlPlaneDataStore
    {
        Task<IEnumerable<IdentityProvider>> GetAllIdentityProvidersAsync(CancellationToken cancellationToken);

        Task<IdentityProvider> GetIdentityProviderAsync(string name, CancellationToken cancellationToken);

        Task<IdentityProvider> UpsertIdentityProviderAsync(IdentityProvider identityProvider, WeakETag weakETag, CancellationToken cancellationToken);

        Task DeleteIdentityProviderAsync(string name, CancellationToken cancellationToken);

        Task<IEnumerable<Role>> GetAllRolesAsync(CancellationToken cancellationToken);

        Task<Role> GetRoleAsync(string name, CancellationToken cancellationToken);

        Task<Role> UpsertRoleAsync(Role role, WeakETag weakETag, CancellationToken cancellationToken);

        Task DeleteRoleAsync(string name, CancellationToken cancellationToken);
    }
}
