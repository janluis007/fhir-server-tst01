// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;

namespace Microsoft.Health.Fhir.Api.Features.Validate
{
    public class ProfileResolver : IResourceResolver
    {
        private CachedResolver _resolver;

        public ProfileResolver()
        {
            _resolver = new CachedResolver(ZipSource.CreateValidationSource());
        }

        public Resource ResolveByUri(string uri)
        {
            return _resolver.ResolveByUri(uri);
        }

        public Resource ResolveByCanonicalUri(string uri)
        {
            return _resolver.ResolveByCanonicalUri(uri);
        }
    }
}
