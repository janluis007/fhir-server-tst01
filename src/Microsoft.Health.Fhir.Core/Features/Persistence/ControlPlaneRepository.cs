// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Fhir.Core.Features.Security;

namespace Microsoft.Health.Fhir.Core.Features.Persistence
{
    public class ControlPlaneRepository : IControlPlaneRepository
    {
        private readonly IControlPlaneDataStore _dataStore;

        public ControlPlaneRepository(IControlPlaneDataStore dataStore)
        {
            EnsureArg.IsNotNull(dataStore, nameof(dataStore));

            _dataStore = dataStore;
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync(CancellationToken cancellationToken)
        {
            return await _dataStore.GetAllRolesAsync(cancellationToken);
        }

        public async Task DeleteIdentityProviderAsync(string name, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

            await _dataStore.DeleteIdentityProviderAsync(name, cancellationToken);
        }

        public async Task<Role> GetRoleAsync(string name, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

            return await _dataStore.GetRoleAsync(name, cancellationToken);
        }

        public async Task<Role> UpsertRoleAsync(Role role, WeakETag weakETag, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(role, nameof(role));

            return await _dataStore.UpsertRoleAsync(role, weakETag, cancellationToken);
        }

        public async Task DeleteRoleAsync(string name, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

            await _dataStore.DeleteRoleAsync(name, cancellationToken);
        }

        public async Task<IEnumerable<IdentityProvider>> GetAllIdentityProvidersAsync(CancellationToken cancellationToken)
        {
            return await _dataStore.GetAllIdentityProvidersAsync(cancellationToken);
        }

        public async Task<IdentityProvider> GetIdentityProviderAsync(string name, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

            return await _dataStore.GetIdentityProviderAsync(name, cancellationToken);
        }

        public async Task<IdentityProvider> UpsertIdentityProviderAsync(IdentityProvider identityProvider, WeakETag weakETag, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(identityProvider, nameof(identityProvider));

            return await _dataStore.UpsertIdentityProviderAsync(identityProvider, weakETag, cancellationToken);
        }
    }
}
