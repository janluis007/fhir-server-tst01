// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Patch
{
    public class PatchOperation
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }
    }
}
