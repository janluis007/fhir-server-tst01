// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.CosmosDb.Features.Storage;
using Microsoft.Health.Fhir.Core.Features.Operations;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Storage.Operations.Import
{
    /// <summary>
    /// A wrapper around the <see cref="ImportJobRecord"/> class that contains metadata specific to CosmosDb.
    /// </summary>
    internal class CosmosImportJobRecordWrapper : SystemData
    {
        public CosmosImportJobRecordWrapper(ImportJobRecord exportJobRecord)
        {
            EnsureArg.IsNotNull(exportJobRecord, nameof(exportJobRecord));

            JobRecord = exportJobRecord;
            Id = exportJobRecord.Id;
        }

        [JsonConstructor]
        protected CosmosImportJobRecordWrapper()
        {
        }

        [JsonProperty(JobRecordProperties.JobRecord)]
        public ImportJobRecord JobRecord { get; private set; }

        [JsonProperty(KnownDocumentProperties.PartitionKey)]
        public string PartitionKey { get; } = CosmosDbImportConstants.ImportJobPartitionKey;
    }
}
