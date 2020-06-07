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

namespace Microsoft.Health.Fhir.Core.Serialization
{
    public class FhirJsonTextNode2 : BaseSourceNode<ResourceBase>
    {
        private const string _idProperty = "id";
        private const string _metaProperty = "meta";
        private readonly string _name;

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
            ResourceBase resource = JsonSerializer.Deserialize<ResourceBase>(json);
            return new FhirJsonTextNode2(resource, name);
        }

        public static async ValueTask<ISourceNode> Parse(Stream jsonReader, string name = null)
        {
            ResourceBase resource = await JsonSerializer.DeserializeAsync<ResourceBase>(jsonReader);
            return new FhirJsonTextNode2(resource, name);
        }

        public string ToRawJson()
        {
            return JsonSerializer.Serialize(Resource);
        }

        protected override IEnumerable<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> PropertySourceNodes()
        {
            yield return (_idProperty, new Lazy<IEnumerable<ISourceNode>>(() => new[] { new FhirStringSourceNode(() => Resource.Id, _idProperty, $"{Location}.{_idProperty}") }));
            yield return (_metaProperty, new Lazy<IEnumerable<ISourceNode>>(() => new[] { new MetaSourceNode(Resource.Meta, _metaProperty, $"{Location}.{_metaProperty}") }));
        }
    }
}
