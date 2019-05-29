// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Core.Features.Persistence;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export
{
    /// <summary>
    /// A serializer used to serialize <see cref="RawResource.Data"/> to bytes representing new line deliminated JSON.
    /// </summary>
    public interface IResourceToNdjsonBytesSerializer
    {
        /// <summary>
        /// Serializes <see cref="RawResource.Data"/> to bytes representing new line deliminated JSON.
        /// </summary>
        /// <param name="rawResource">The raw resource to serialize.</param>
        /// <returns>The serialized bytes.</returns>
        byte[] Serialize(RawResource rawResource);
    }
}
