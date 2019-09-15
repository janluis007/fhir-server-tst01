// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import.Models
{
    public class ImportRequestEntry
    {
        public ImportRequestEntry()
        {
        }

        [JsonProperty("type")]
        public string Type { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "Uri type cannot be serialized.")]
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }
    }
}
