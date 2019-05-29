// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Core.Features.Persistence;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export
{
    /// <summary>
    /// A serializer used to serialize an instance of <see cref="ResourceWrapper"/> to bytes representing new line deliminated JSON.
    /// </summary>
    public class ResourceToNdjsonBytesSerializer : IResourceToNdjsonBytesSerializer
    {
        private readonly ResourceDeserializer _resourceDeserializer;
        private readonly FhirJsonSerializer _fhirJsonSerializer;

        public ResourceToNdjsonBytesSerializer(ResourceDeserializer resourceDeserializer, FhirJsonSerializer fhirJsonSerializer)
        {
            EnsureArg.IsNotNull(resourceDeserializer, nameof(resourceDeserializer));
            EnsureArg.IsNotNull(fhirJsonSerializer, nameof(fhirJsonSerializer));

            _resourceDeserializer = resourceDeserializer;
            _fhirJsonSerializer = fhirJsonSerializer;
        }

        /// <inheritdoc />
        public byte[] Serialize(RawResource rawResource)
        {
            EnsureArg.IsNotNull(rawResource, nameof(rawResource));

            string resourceData = null;

            if (rawResource.Format == Hl7.Fhir.Rest.ResourceFormat.Json)
            {
                // This is JSON already, we can write as is.
                resourceData = rawResource.Data;
            }
            else
            {
                // This is not JSON, so deserialize it and serialize it to JSON.
                Resource resource = _resourceDeserializer.DeserializeRaw(rawResource);

                resourceData = _fhirJsonSerializer.SerializeToString(resource);
            }

            byte[] bytesToWrite = Encoding.UTF8.GetBytes($"{resourceData}\n");

            return bytesToWrite;
        }
    }
}
