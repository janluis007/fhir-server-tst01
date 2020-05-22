// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;
using EnsureThat;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export
{
    /// <summary>
    /// A serializer used to serialize the resource represented by <see cref="ResourceWrapper"/> to byte array representing new line deliminated JSON.
    /// </summary>
    public class ResourceToNdjsonBytesSerializer : IResourceToByteArraySerializer
    {
        private readonly ResourceDeserializer _resourceDeserializer;

        public ResourceToNdjsonBytesSerializer(ResourceDeserializer resourceDeserializer)
        {
            EnsureArg.IsNotNull(resourceDeserializer, nameof(resourceDeserializer));

            _resourceDeserializer = resourceDeserializer;
        }

        /// <inheritdoc />
        public byte[] Serialize(ResourceWrapper resourceWrapper)
        {
            EnsureArg.IsNotNull(resourceWrapper, nameof(resourceWrapper));

            ResourceElement resource = _resourceDeserializer.DeserializeRaw(resourceWrapper.RawResource, resourceWrapper.Version, resourceWrapper.LastModified);

            string resourceData = resource.ToJson();

            byte[] bytesToWrite = Encoding.UTF8.GetBytes($"{resourceData}\n");

            return bytesToWrite;
        }
    }
}
