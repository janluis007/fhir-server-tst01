// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Storage.StoredProcedures.Upsert
{
    internal class UpsertWithHistoryModel
    {
        [JsonConstructor]
        protected UpsertWithHistoryModel()
        {
        }

        [JsonProperty("outcomeType")]
        [JsonPropertyName("outcomeType")]
        public SaveOutcomeType OutcomeType { get; set; }

        [JsonProperty("wrapper")]
        [JsonPropertyName("wrapper")]
        public FhirCosmosResourceWrapper Wrapper { get; set; }
    }
}
