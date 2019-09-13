// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import.Models
{
    /// <summary>
    /// Represents metadata required for each file that is generated as part of the
    /// export operation.
    /// </summary>
    public class ImportEntryInfo
    {
        public ImportEntryInfo(
            string type,
            int count)
        {
            EnsureArg.IsNotNullOrWhiteSpace(type);

            Type = type;
            Count = count;
        }

        [JsonConstructor]
        protected ImportEntryInfo()
        {
        }

        [JsonProperty(JobRecordProperties.Type)]
        public string Type { get; private set; }

        [JsonProperty(JobRecordProperties.Count)]
        public int Count { get; private set; }
    }
}
