// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.CosmosDb.Features.Storage;

namespace Microsoft.Health.Fhir.Tests.E2E.ChaosMonkey
{
    public class ChaosFhirCosmosClientInitializer : FhirCosmosClientInitializer
    {
        public ChaosFhirCosmosClientInitializer(ICosmosClientTestProvider testProvider, Func<IEnumerable<RequestHandler>> requestHandlerFactory, RetryExceptionPolicyFactory retryExceptionPolicyFactory, ILogger<FhirCosmosClientInitializer> logger)
            : base(testProvider, requestHandlerFactory, retryExceptionPolicyFactory, logger)
        {
        }

        public override Container CreateFhirContainer(CosmosClient client, string databaseId, string collectionId)
        {
            return new ChaosContainer(base.CreateFhirContainer(client, databaseId, collectionId));
        }
    }
}
