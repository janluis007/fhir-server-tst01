// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Features.Persistence
{
    /// <summary>
    /// Provides a mechanism to create a <see cref="RawResource"/>
    /// </summary>
    public class RawResourceFactory : IRawResourceFactory
    {
        /// <inheritdoc />
        public RawResource Create(ResourceElement resource)
        {
            EnsureArg.IsNotNull(resource, nameof(resource));

            var versionId = resource.VersionId;
            var lastUpdated = resource.LastUpdated;

            try
            {
                // Clear meta version and lastUpdated since these are set based on generated values when saving the resource
                resource.UpdateVersion(null);
                resource.UpdateLastUpdated(null);

                return new RawResource(resource.ToJson(), FhirResourceFormat.Json);
            }
            finally
            {
                resource.UpdateVersion(versionId);
                resource.UpdateLastUpdated(lastUpdated);
            }
        }
    }
}
