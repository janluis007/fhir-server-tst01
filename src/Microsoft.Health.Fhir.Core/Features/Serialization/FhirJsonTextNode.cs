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
using Microsoft.Health.Fhir.Core.Features.Serialization.SourceNodes;
using Microsoft.Health.Fhir.Core.Features.Serialization.SourceNodes.Models;

namespace Microsoft.Health.Fhir.Core.Features.Serialization
{
    public class FhirJsonTextNode : BaseSourceNode<ResourceJsonNode>, ISerializeToJson
    {
        private const string _idProperty = "id";
        private const string _metaProperty = "meta";
        private readonly string _name;

        private static readonly JsonSerializerOptions _indentedOptions = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
            WriteIndented = true,
        };

        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
        };

        internal FhirJsonTextNode(ResourceJsonNode resource, string name)
            : base(resource)
        {
            _name = name;
        }

        public override string Name => _name ?? Resource.ResourceType;

        public override string Text => null;

        public override string Location => ResourceType;

        public override string ResourceType => Resource.ResourceType ?? _name;

        public string SerializeToJson(bool writeIndented = false)
        {
            return JsonSerializer.Serialize(Resource, Resource.GetType(), writeIndented ? _indentedOptions : _options);
        }

        public async Task SerializeToJson(Stream stream, bool writeIndented = false)
        {
            await JsonSerializer.SerializeAsync(stream, Resource, Resource.GetType(), writeIndented ? _indentedOptions : _options)
                .ConfigureAwait(false);
        }

        protected override IEnumerable<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> PropertySourceNodes()
        {
            yield return (_idProperty, new Lazy<IEnumerable<ISourceNode>>(() => new[] { new FhirStringSourceNode(() => Resource.Id, _idProperty, $"{Location}.{_idProperty}") }));
            yield return (_metaProperty, new Lazy<IEnumerable<ISourceNode>>(() => new[] { new MetaSourceNode(Resource.Meta, _metaProperty, $"{Location}.{_metaProperty}") }));
        }
    }
}
