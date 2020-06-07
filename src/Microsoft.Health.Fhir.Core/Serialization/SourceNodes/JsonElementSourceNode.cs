// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Utility;

namespace Microsoft.Health.Fhir.Core.Serialization.SourceNodes
{
    internal class JsonElementSourceNode : ISourceNode, IResourceTypeSupplier, IAnnotated
    {
        private readonly JsonElement? _valueElement;
        private readonly JsonElement? _contentElement;
        private readonly string _name;
        private readonly int? _arrayIndex;
        private readonly string _location;
        private IList<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> _cachedNodes;

        public JsonElementSourceNode(JsonElement? valueElement, JsonElement? contentElement, string name, int? arrayIndex, string location)
        {
            _valueElement = valueElement;
            _contentElement = contentElement;
            _name = name;
            _arrayIndex = arrayIndex;
            _location = location;
        }

        public string Name => _name;

        public string Text
        {
            get
            {
                if (_valueElement?.ValueKind == JsonValueKind.String)
                {
                    if (_valueElement?.GetString() != null)
                    {
                        return _valueElement?.GetString().Trim();
                    }
                }

                if (_valueElement?.ValueKind == JsonValueKind.Object || _valueElement?.ValueKind == JsonValueKind.Array || _valueElement?.ValueKind == JsonValueKind.Undefined)
                {
                    return null;
                }

                if (_valueElement != null)
                {
                    string rawText = _valueElement?.GetRawText();
                    if (!string.IsNullOrWhiteSpace(rawText))
                    {
                        return PrimitiveTypeConverter.ConvertTo<string>(rawText.Trim());
                    }
                }

                return null;
            }
        }

        public string Location => _location;

        public string ResourceType => _contentElement != null ? GetResourceTypePropertyFromObject(_contentElement.Value, _name)?.GetString() : null;

        public IEnumerable<object> Annotations(Type type)
        {
            if (type == typeof(FhirJsonTextNode2) || type == typeof(ISourceNode) || type == typeof(IResourceTypeSupplier))
            {
                return new[] { this };
            }

            return Enumerable.Empty<object>();
        }

        public IEnumerable<ISourceNode> Children(string name = null)
        {
            if (_cachedNodes == null)
            {
                var list = new List<(string, Lazy<IEnumerable<ISourceNode>>)>();

                if (!(_contentElement == null ||
                      _contentElement?.ValueKind != JsonValueKind.Object
                      || _contentElement?.EnumerateObject().Any() == false))
                {
                    var objectEnumerator = _contentElement.GetValueOrDefault().EnumerateObject().Select(x => (x.Name, x.Value));
                    list.AddRange(ProcessObjectProperties(objectEnumerator, _location));
                }

                _cachedNodes = list;
            }

            if (name == null)
            {
                return _cachedNodes.SelectMany(x => x.Node.Value);
            }

            return _cachedNodes
                .Where(x => string.Equals(name, x.Name, StringComparison.Ordinal))
                .SelectMany(x => x.Node.Value);
        }

        internal static List<(string, Lazy<IEnumerable<ISourceNode>>)> ProcessObjectProperties(IEnumerable<(string Name, JsonElement Value)> objectEnumerator, string location)
        {
            var list = new List<(string, Lazy<IEnumerable<ISourceNode>>)>();

            foreach (var item in objectEnumerator
                .GroupBy(x => x.Name.TrimStart('_'))
                .Where(x => !string.Equals(x.Key, "resourceType", StringComparison.OrdinalIgnoreCase)))
            {
                if (item.Count() == 1)
                {
                    var innerItem = item.First();
                    var values = (innerItem.Name, new Lazy<IEnumerable<ISourceNode>>(() => JsonElementToSourceNodes(innerItem.Name, location, innerItem.Value)));
                    list.Add(values);
                }
                else if (item.Count() == 2)
                {
                    var fistInnerItem = item.SingleOrDefault(x => !x.Name.Contains("_", StringComparison.Ordinal));
                    var lastInnerItem = item.SingleOrDefault(x => x.Name.Contains("_", StringComparison.Ordinal));
                    var values = (fistInnerItem.Name, new Lazy<IEnumerable<ISourceNode>>(() => JsonElementToSourceNodes(fistInnerItem.Name, location, fistInnerItem.Value, lastInnerItem.Value)));
                    list.Add(values);
                }
                else
                {
                    throw new Exception($"Expected 1 or two nodes with name '{item.Key}'");
                }
            }

            return list;
        }

        internal static IEnumerable<ISourceNode> JsonElementToSourceNodes(string name, string location, JsonElement item, JsonElement? shadowItem = null)
        {
            (IReadOnlyList<JsonElement> List, bool ArrayProperty) itemList = ToList(item);
            (IReadOnlyList<JsonElement> List, bool ArrayProperty)? shadowItemList = shadowItem != null ? ((IReadOnlyList<JsonElement> List, bool ArrayProperty)?)ToList(shadowItem.Value) : (Array.Empty<JsonElement>(), false);

            var isArray = itemList.ArrayProperty | shadowItemList?.ArrayProperty ?? false;
            for (int i = 0; i < Math.Max(itemList.List.Count, shadowItemList?.List.Count ?? 0); i++)
            {
                var first = At(itemList.List, i);
                var shadow = At(shadowItemList?.List, i);
                JsonElement? content = null;
                JsonElement? value = null;

                if (first?.ValueKind == JsonValueKind.Object)
                {
                    content = first;
                    value = shadow;
                }
                else
                {
                    content = shadow;
                    value = first;
                }

                var arrayText = isArray ? $"[{i}]" : null;
                var itemLocation = $"{location}.{name}{arrayText}";

                yield return new JsonElementSourceNode(
                    value,
                    content,
                    name,
                    itemList.ArrayProperty ? i : (int?)null,
                    itemLocation);
            }

            (IReadOnlyList<JsonElement> List, bool ArrayProperty) ToList(JsonElement prop)
            {
                if (prop.ValueKind == JsonValueKind.Null)
                {
                    return (Array.Empty<JsonElement>(), false);
                }

                if (prop.ValueKind == JsonValueKind.Array)
                {
                    return (prop.EnumerateArray().Select(x => x).ToArray(), true);
                }

                return (new[] { prop }, false);
            }

            JsonElement? At(IReadOnlyList<JsonElement> list, int i) => list?.Count > i ? (JsonElement?)list[i] : null;
        }

        private static JsonElement? GetResourceTypePropertyFromObject(JsonElement o, string name)
        {
            return !o.TryGetProperty(JsonSerializationDetails.RESOURCETYPE_MEMBER_NAME, out var type) ? null
                : type.ValueKind == JsonValueKind.String && name != "instance" ? (JsonElement?)type : null;
        }
    }
}
