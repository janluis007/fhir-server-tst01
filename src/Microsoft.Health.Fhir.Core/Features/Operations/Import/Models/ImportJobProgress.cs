// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import.Models
{
    public class ImportJobProgress
    {
        public ImportJobProgress()
        {
        }

        [JsonProperty("bytesProcessed")]
        public long BytesProcessed { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("isComplete")]
        public bool IsComplete { get; set; }
    }
}
