// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import.Models
{
    public class ImportRequestEntryStorageDetail
    {
        public ImportRequestEntryStorageDetail(string type)
        {
            EnsureArg.IsNotNullOrWhiteSpace(type, nameof(type));

            Type = type;
        }

        [JsonConstructor]
        protected ImportRequestEntryStorageDetail()
        {
        }

        [JsonProperty("type")]
        public string Type { get; private set; }
    }
}
