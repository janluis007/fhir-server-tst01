// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Core.Features.Serialization.SourceNodes.Models;

namespace Microsoft.Health.Fhir.Core.Features.Serialization
{
    public static class JsonSourceNodeFactory
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = false,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Disallow,
        };

        public static ISourceNode Parse(string json, string name = null)
        {
            ResourceJsonNode resource = JsonSerializer.Deserialize<ResourceJsonNode>(json, _jsonSerializerOptions);
            return new FhirJsonTextNode(resource, name);
        }

        public static async ValueTask<ISourceNode> Parse(Stream jsonReader, string name = null)
        {
            ResourceJsonNode resource = await JsonSerializer.DeserializeAsync<ResourceJsonNode>(jsonReader, _jsonSerializerOptions);
            return new FhirJsonTextNode(resource, name);
        }

        public static ISourceNode Create(ResourceJsonNode resource)
        {
            return new FhirJsonTextNode(resource, null);
        }
    }
}
