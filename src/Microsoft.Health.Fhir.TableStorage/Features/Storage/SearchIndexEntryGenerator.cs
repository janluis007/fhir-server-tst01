// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Core.Extensions;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Features.Search.SearchValues;
using Microsoft.Health.Fhir.TableStorage.Configs;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Health.Fhir.TableStorage.Features.Storage
{
    internal class SearchIndexEntryGenerator : ISearchValueVisitor
    {
        private readonly TableStorageDataStoreConfiguration _config;
        private readonly List<IndexEntry> _generatedObjects = new List<IndexEntry>();
        private readonly IDictionary<string, int> _propertyIndex = new Dictionary<string, int>();

        public SearchIndexEntryGenerator(TableStorageDataStoreConfiguration config)
        {
            _config = config;
        }

        private SearchIndexEntry Entry { get; set; }

        private int? Index { get; set; }

        private bool IsCompositeComponent => Index != null;

        private IndexEntry CurrentEntry { get; set; }

        public IDictionary<string, EntityProperty> Generate(IEnumerable<SearchIndexEntry> searchIndexes)
        {
            foreach (var item in searchIndexes)
            {
                Add(item);
            }

            return Generate();
        }

        private void Add(SearchIndexEntry entry)
        {
            EnsureArg.IsNotNull(entry, nameof(entry));

            Entry = entry;
            CurrentEntry = null;

            entry.Value.AcceptVisitor(this);
        }

        private IDictionary<string, EntityProperty> Generate()
        {
            var groups = _generatedObjects
                .GroupBy(x => x.Name + string.Join("||", x.Parts.OrderBy(y => y.Key).Select(y => $"{y.Key}:{FormatValue(y.Value)}")));

            if (Debugger.IsAttached)
            {
                foreach (var dup in groups.Where(x => x.Count() > 1))
                {
                    Debug.WriteLine("Excluding dup ({0}): {1}", dup.Count(), dup.Key);
                }
            }

            // TableStorage limit of 256 columns.

            var pocoColumns = typeof(FhirTableEntity)
                .GetProperties()
                .Count(x => x.CanWrite);

            var si = groups
                .SelectMany(x => x.First().ToProperty(GetIndex, _config.MaxIndexCombinationsPerType))
                .Take(256 - pocoColumns)
                .ToDictionary(x => x.Key, x => x.Value);

            return si;
        }

        private static object FormatValue(object y)
        {
            if (y is DateTimeOffset dt)
            {
                return dt.ToString("o");
            }

            return y?.ToString();
        }

        void ISearchValueVisitor.Visit(CompositeSearchValue composite)
        {
            foreach (IEnumerable<ISearchValue> componentValues in composite.Components.CartesianProduct())
            {
                int index = 0;
                CreateEntry();

                try
                {
                    foreach (ISearchValue componentValue in componentValues)
                    {
                        // Set the component index and process individual component of the composite value.
                        Index = index++;

                        componentValue.AcceptVisitor(this);
                    }
                }
                finally
                {
                    Index = null;
                }
            }
        }

        void ISearchValueVisitor.Visit(DateTimeSearchValue dateTime)
        {
            AddProperty(SearchValueConstants.DateTimeStartName, dateTime.Start);
            AddProperty(SearchValueConstants.DateTimeEndName, dateTime.End);
        }

        void ISearchValueVisitor.Visit(NumberSearchValue number)
        {
            if (number.Low == number.High)
            {
                AddProperty(SearchValueConstants.NumberName, number.Low);
            }

            AddProperty(SearchValueConstants.LowNumberName, number.Low);
            AddProperty(SearchValueConstants.HighNumberName, number.High);
        }

        void ISearchValueVisitor.Visit(QuantitySearchValue quantity)
        {
            AddPropertyIfNotNull(SearchValueConstants.SystemName, quantity.System);
            AddPropertyIfNotNull(SearchValueConstants.CodeName, quantity.Code);

            if (quantity.Low == quantity.High)
            {
                AddProperty(SearchValueConstants.QuantityName, quantity.Low);
            }

            AddProperty(SearchValueConstants.LowQuantityName, quantity.Low);
            AddProperty(SearchValueConstants.HighQuantityName, quantity.High);
        }

        void ISearchValueVisitor.Visit(ReferenceSearchValue reference)
        {
            AddPropertyIfNotNull(SearchValueConstants.ReferenceBaseUriName, reference.BaseUri?.ToString());
            AddPropertyIfNotNull(SearchValueConstants.ReferenceResourceTypeName, reference.ResourceType);
            AddProperty(SearchValueConstants.ReferenceResourceIdName, reference.ResourceId);
        }

        void ISearchValueVisitor.Visit(StringSearchValue s)
        {
            if (!IsCompositeComponent)
            {
                AddProperty(SearchValueConstants.StringName, s.String);
            }

            AddProperty(SearchValueConstants.NormalizedStringName, s.String.ToUpperInvariant());
        }

        void ISearchValueVisitor.Visit(TokenSearchValue token)
        {
            AddPropertyIfNotNull(SearchValueConstants.SystemName, token.System);
            AddPropertyIfNotNull(SearchValueConstants.CodeName, token.Code);

            if (!IsCompositeComponent)
            {
                // Since text is case-insensitive search, it will always be normalized.
                AddPropertyIfNotNull(SearchValueConstants.NormalizedTextName, token.Text?.ToUpperInvariant());
            }
        }

        void ISearchValueVisitor.Visit(UriSearchValue uri)
        {
            AddProperty(SearchValueConstants.UriName, uri.Uri);
        }

        private void CreateEntry()
        {
            CurrentEntry = new IndexEntry(Entry.SearchParameter.Name);

            _generatedObjects.Add(CurrentEntry);
        }

        private void AddProperty(string name, object value)
        {
            if (CurrentEntry == null)
            {
                CreateEntry();
            }

            var cn = name;
            if (IsCompositeComponent)
            {
                cn = $"c{Index}_{name}";
            }

            CurrentEntry.Parts.Add(cn, value);
        }

        private void AddPropertyIfNotNull(string name, string value)
        {
            if (value != null)
            {
                AddProperty(name, value);
            }
        }

        private int GetIndex(string key)
        {
            if (_propertyIndex.ContainsKey(key))
            {
                return ++_propertyIndex[key];
            }

            return _propertyIndex[key] = 0;
        }

        private class IndexEntry
        {
            public IndexEntry(string name)
            {
                Name = name.Replace("-", string.Empty, StringComparison.Ordinal);
                Parts = new Dictionary<string, object>();
            }

            public IDictionary<string, object> Parts { get; }

            public string Name { get; }

            public IEnumerable<KeyValuePair<string, EntityProperty>> ToProperty(Func<string, int> nameIndex, int maxIndexCombinationsPerType)
            {
                int index = nameIndex(Name);

                if (index > maxIndexCombinationsPerType)
                {
                    yield break;
                }

                foreach (var props in Parts)
                {
                    string name = $"s_{Name}{index}_{props.Key}";

                    Debug.WriteLine($"Adding index: {name}={props.Value}");

                    yield return new KeyValuePair<string, EntityProperty>(
                        name,
                        EntityProperty.CreateEntityPropertyFromObject(MapEntityValue(props)));
                }
            }

            private static object MapEntityValue(KeyValuePair<string, object> props)
            {
                if (props.Value is decimal)
                {
                    // TableStorage does not support decimal
                    return (double)(decimal)props.Value;
                }

                return props.Value;
            }
        }
    }
}
