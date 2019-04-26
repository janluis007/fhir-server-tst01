// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Health.CosmosDb.Features.Storage.StoredProcedures;
using Microsoft.Health.Fhir.CosmosDb.Features.Storage.Operations.Import;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Storage.StoredProcedures.AcquireImportJobs
{
    internal class AcquireImportJobs : StoredProcedureBase, IFhirStoredProcedure
    {
        public async Task<StoredProcedureResponse<IReadOnlyCollection<CosmosImportJobRecordWrapper>>> ExecuteAsync(
            IDocumentClient client,
            Uri collectionUri,
            ushort maximumNumberOfConcurrentJobsAllowed,
            ushort jobHeartbeatTimeoutThresholdInSeconds,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(collectionUri, nameof(collectionUri));

            return await ExecuteStoredProc<IReadOnlyCollection<CosmosImportJobRecordWrapper>>(
                client,
                collectionUri,
                CosmosDbImportConstants.ImportJobPartitionKey,
                cancellationToken,
                maximumNumberOfConcurrentJobsAllowed,
                jobHeartbeatTimeoutThresholdInSeconds);
        }
    }
}
