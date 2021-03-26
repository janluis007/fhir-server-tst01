// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Features.Search.SearchValues;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.CosmosDb.Features.Search;
using Microsoft.Health.Fhir.ValueSets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Storage.Search
{
    public class SearchIndexEntryConverter : JsonConverter
    {
        private static readonly ConcurrentQueue<SearchIndexEntryJObjectGenerator> CachedGenerators = new ConcurrentQueue<SearchIndexEntryJObjectGenerator>();

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SearchIndexEntry) || objectType == typeof(IReadOnlyCollection<SearchIndexEntry>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // We don't currently support reading the search index from the Cosmos DB.
            JArray array = JArray.Load(reader);
            var result = new List<SearchIndexEntry>();
            foreach (var jToken in array)
            {
                var jObject = jToken as JObject;
                if (jObject["p"].ToString() == "_id")
                {
                    continue;
                }

                var parameterName = jObject[SearchValueConstants.ParamName].ToString();
                var compositeSearchValue = TryParseCompositeSearchValues(jObject);
                if (compositeSearchValue == null)
                {
                    var parseResult = ParseNonCompositeSearchValue(jObject);
                    if (parseResult.Item2 != null)
                    {
                        var searchParameter = new SearchParameterInfo(parameterName, parameterName, parseResult.Item1);
                        result.Add(new SearchIndexEntry(searchParameter, parseResult.Item2));
                    }
                }
                else
                {
                    var searchParameter = new SearchParameterInfo(parameterName, parameterName, SearchParamType.Composite);
                    result.Add(new SearchIndexEntry(searchParameter, compositeSearchValue));
                }
            }

            return result;
        }

        private static Tuple<SearchParamType, ISearchValue> ParseNonCompositeSearchValue(JObject jObject)
        {
            ISearchValue value = null;
            SearchParamType type = SearchParamType.Date;

            if (jObject.ContainsKey(SearchValueConstants.DateTimeStartName))
            {
                type = SearchParamType.Date;
                var startTime = (DateTimeOffset)jObject[SearchValueConstants.DateTimeStartName];
                var endTime = (DateTimeOffset)jObject[SearchValueConstants.DateTimeEndName];

                value = new DateTimeSearchValue(new PartialDateTime(startTime), new PartialDateTime(endTime));
            }
            else if (jObject.ContainsKey(SearchValueConstants.NormalizedTextName))
            {
                type = SearchParamType.Token;
                value = new TokenSearchValue(jObject[SearchValueConstants.SystemName].ToString(), jObject[SearchValueConstants.CodeName].ToString(), jObject[SearchValueConstants.NormalizedTextName].ToString());
            }
            else if (jObject.ContainsKey(SearchValueConstants.NormalizedStringName))
            {
                type = SearchParamType.String;
                value = new StringSearchValue(jObject[SearchValueConstants.NormalizedStringName].ToString());
            }
            else if (jObject.ContainsKey(SearchValueConstants.LowNumberName))
            {
                type = SearchParamType.Number;
                var lowNumber = (decimal)jObject[SearchValueConstants.LowNumberName];
                var highNumber = (decimal)jObject[SearchValueConstants.HighNumberName];
                value = new NumberSearchValue(lowNumber, highNumber);
            }
            else if (jObject.ContainsKey(SearchValueConstants.LowQuantityName))
            {
                type = SearchParamType.Quantity;
                string system = null;
                string code = null;

                if (jObject.ContainsKey(SearchValueConstants.SystemName))
                {
                    system = jObject[SearchValueConstants.SystemName].ToString();
                }

                if (jObject.ContainsKey(SearchValueConstants.CodeName))
                {
                    code = jObject[SearchValueConstants.CodeName].ToString();
                }

                var lowQuantity = (decimal)jObject[SearchValueConstants.LowQuantityName];
                var highQuantity = (decimal)jObject[SearchValueConstants.HighQuantityName];
                value = new QuantitySearchValue(system, code, lowQuantity, highQuantity);
            }
            else if (jObject.ContainsKey(SearchValueConstants.ReferenceResourceIdName))
            {
                type = SearchParamType.Reference;

                Uri uri = null;
                string resourceType = null;
                if (jObject.ContainsKey(SearchValueConstants.ReferenceBaseUriName))
                {
                    uri = new Uri(jObject[SearchValueConstants.ReferenceBaseUriName].ToString());
                }

                if (jObject.ContainsKey(SearchValueConstants.ReferenceResourceTypeName))
                {
                    resourceType = jObject[SearchValueConstants.ReferenceResourceTypeName].ToString();
                }

                var resourceId = jObject[SearchValueConstants.ReferenceResourceIdName].ToString();
                value = new ReferenceSearchValue(ReferenceKind.InternalOrExternal, uri, resourceType, resourceId);
            }
            else if (jObject.ContainsKey(SearchValueConstants.UriName))
            {
                type = SearchParamType.Uri;

                string uri = jObject[SearchValueConstants.UriName].ToString();
                value = new UriSearchValue(uri, false);
            }
            else if (jObject.ContainsKey(SearchValueConstants.CodeName))
            {
                type = SearchParamType.Uri;
                string system = null;

                if (jObject.ContainsKey(SearchValueConstants.SystemName))
                {
                    system = jObject[SearchValueConstants.SystemName].ToString();
                }

                value = new TokenSearchValue(system, jObject[SearchValueConstants.CodeName].ToString(), string.Empty);
            }

            return new Tuple<SearchParamType, ISearchValue>(type, value);
        }

        private CompositeSearchValue TryParseCompositeSearchValues(JObject jObject)
        {
            var compositeObjects = new Dictionary<int, JObject>();
            char seperator = '_';
            foreach (var property in jObject.Properties())
            {
                int seperatorIndex = property.Name.LastIndexOf(seperator);
                if (seperatorIndex != -1 && seperatorIndex < property.Name.Length - 1)
                {
                    var propertyIndexString = property.Name.Substring(seperatorIndex + 1);
                    if (int.TryParse(propertyIndexString, out int propertyIndex))
                    {
                        var propertyName = property.Name.Substring(0, seperatorIndex);
                        if (!compositeObjects.ContainsKey(propertyIndex))
                        {
                            compositeObjects[propertyIndex] = new JObject();
                        }

                        compositeObjects[propertyIndex].Add(propertyName, property.Value);
                    }
                }
            }

            var numberOfComponents = compositeObjects.Count();
            if (numberOfComponents == 0)
            {
                return null;
            }

            var componentValues = new IReadOnlyList<ISearchValue>[numberOfComponents];
            for (int i = 0; i < numberOfComponents; i++)
            {
                if (!compositeObjects.ContainsKey(i))
                {
                    return null;
                }

                var searchValue = ParseNonCompositeSearchValue(compositeObjects[i]);
                componentValues[i] = new List<ISearchValue> { searchValue.Item2 };
            }

            return new CompositeSearchValue(componentValues);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var searchIndexEntry = (SearchIndexEntry)value;

            // Cached the object generator for reuse.
            if (!CachedGenerators.TryDequeue(out SearchIndexEntryJObjectGenerator generator))
            {
                generator = new SearchIndexEntryJObjectGenerator();
            }

            IReadOnlyList<JObject> generatedObjects;

            try
            {
                generatedObjects = generator.Generate(searchIndexEntry);

                foreach (JObject generatedObj in generatedObjects)
                {
                    generatedObj.WriteTo(writer);
                }
            }
            finally
            {
                CachedGenerators.Enqueue(generator);
            }
        }
    }
}
