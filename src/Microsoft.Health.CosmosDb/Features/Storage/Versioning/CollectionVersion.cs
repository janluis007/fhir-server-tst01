// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Health.CosmosDb.Features.Storage.Versioning
{
    public class CollectionVersion : SystemData
    {
        public const string CollectionVersionPartition = "_collectionVersions";

        public CollectionVersion()
        {
            Id = "collectionversion";
        }

        [JsonProperty("version")]
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonProperty(KnownDocumentProperties.PartitionKey)]
        [JsonPropertyName(KnownDocumentProperties.PartitionKey)]
        public string PartitionKey { get; } = CollectionVersionPartition;
    }
}
