// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import.Models
{
    public class ImportRequest
    {
        [JsonProperty("inputFormat")]
        public string InputFormat { get; set; }

        [JsonProperty("inputSource")]
        public string InputSource { get; set; }

        [JsonProperty("storageDetail")]
        public ImportRequestStorageDetail StorageDetail { get; set; }

        [JsonProperty("input")]
        public IList<ImportRequestEntry> Input { get; } = new List<ImportRequestEntry>();
    }
}
