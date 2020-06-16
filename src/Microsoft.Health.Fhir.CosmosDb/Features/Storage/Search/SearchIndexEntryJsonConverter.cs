// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.Core.Features.Search;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Storage.Search
{
    public class SearchIndexEntryJsonConverter : JsonConverter<SearchIndexEntry>
    {
        private static readonly ConcurrentQueue<SearchIndexEntryDictionaryGenerator> CachedGenerators = new ConcurrentQueue<SearchIndexEntryDictionaryGenerator>();

        public override SearchIndexEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // We don't currently support reading the search index from the Cosmos DB.
            return null;
        }

        public override void Write(Utf8JsonWriter writer, SearchIndexEntry value, JsonSerializerOptions options)
        {
            var searchIndexEntry = value;

            // Cached the object generator for reuse.
            if (!CachedGenerators.TryDequeue(out SearchIndexEntryDictionaryGenerator generator))
            {
                generator = new SearchIndexEntryDictionaryGenerator();
            }

            try
            {
                var generatedObjects = generator.Generate(searchIndexEntry);

                foreach (var entry in generatedObjects)
                {
                    JsonSerializer.Serialize(writer, entry, options);
                }
            }
            finally
            {
                CachedGenerators.Enqueue(generator);
            }
        }
    }
}
