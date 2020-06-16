// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;
using EnsureThat;
using Microsoft.Health.Fhir.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Health.Fhir.Core.Features.Persistence
{
    public class RawResource
    {
        public RawResource(string data, FhirResourceFormat format)
        {
            EnsureArg.IsNotNullOrEmpty(data, nameof(data));

            Data = data;
            Format = format;
        }

        [JsonConstructor]
        protected RawResource()
        {
        }

        [JsonProperty("data")]
        [JsonPropertyName("data")]
        public string Data { get; protected set; }

        [JsonProperty("format")]
        [JsonPropertyName("format")]
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public FhirResourceFormat Format { get; protected set; }
    }
}
