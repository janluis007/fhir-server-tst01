// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Security;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Storage.ControlPlane
{
    public class CosmosIdentityProvider : IdentityProvider
    {
        internal const string IdentityProviderPartition = "_identityProviders";

        public CosmosIdentityProvider(IdentityProvider identityProvider)
            : base(identityProvider.Name, identityProvider.Authority, identityProvider.Audience)
        {
        }

        [JsonConstructor]
        protected CosmosIdentityProvider()
        {
        }

        [JsonProperty("id")]
        public string Id => Name;

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; } = IdentityProviderPartition;

        [JsonProperty("_etag")]
        public string ETag { get; protected set; }

        [JsonProperty(KnownResourceWrapperProperties.IsSystem)]
        public bool IsSystem { get; } = true;
    }
}
