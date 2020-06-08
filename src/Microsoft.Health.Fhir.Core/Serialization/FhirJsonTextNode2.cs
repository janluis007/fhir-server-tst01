// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Core.Serialization.SourceNodes;
using Microsoft.Health.Fhir.Core.Serialization.SourceNodes.Models;

namespace Microsoft.Health.Fhir.Core.Serialization
{
    public class FhirJsonTextNode2 : BaseSourceNode<ResourceBase>
    {
        private const string _idProperty = "id";
        private const string _metaProperty = "meta";
        private readonly string _name;

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = false,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Disallow,
        };

        private FhirJsonTextNode2(ResourceBase resource, string name)
            : base(resource)
        {
            _name = name;
        }

        public override string Name => _name ?? Resource.ResourceType;

        public override string Text => null;

        public override string Location => ResourceType;

        public override string ResourceType => Resource.ResourceType ?? _name;

        public static ISourceNode Parse(string json, string name = null)
        {
            ResourceBase resource = JsonSerializer.Deserialize<ResourceBase>(json, _jsonSerializerOptions);
            return new FhirJsonTextNode2(resource, name);
        }

        public static async ValueTask<ISourceNode> Parse(Stream jsonReader, string name = null)
        {
            ResourceBase resource = await JsonSerializer.DeserializeAsync<ResourceBase>(jsonReader, _jsonSerializerOptions);
            return new FhirJsonTextNode2(resource, name);
        }

        public static ISourceNode Create(ResourceBase resource)
        {
            return new FhirJsonTextNode2(resource, null);
        }

        public string SerializeToJson(bool writeIndented = false)
        {
            return JsonSerializer.Serialize(Resource, Resource.GetType(), new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = writeIndented,
            });
        }

        public async Task SerializeToJson(Stream stream, bool writeIndented = false)
        {
            await JsonSerializer.SerializeAsync(stream, Resource, Resource.GetType(), new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = writeIndented,
            });
        }

        protected override IEnumerable<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> PropertySourceNodes()
        {
            yield return (_idProperty, new Lazy<IEnumerable<ISourceNode>>(() => new[] { new FhirStringSourceNode(() => Resource.Id, _idProperty, $"{Location}.{_idProperty}") }));
            yield return (_metaProperty, new Lazy<IEnumerable<ISourceNode>>(() => new[] { new MetaSourceNode(Resource.Meta, _metaProperty, $"{Location}.{_metaProperty}") }));
        }
    }
}
