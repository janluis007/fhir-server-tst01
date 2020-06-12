// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;

namespace Microsoft.Health.CosmosDb.Features.Queries
{
    public interface ICosmosResponseProcessor
    {
        Task ProcessException(Exception ex);

        Task<ItemResponse<T>> ProcessResponse<T>(ItemResponse<T> feedResponse);

        Task<FeedResponse<T>> ProcessResponse<T>(FeedResponse<T> feedResponse);

        Task<StoredProcedureExecuteResponse<T>> ProcessResponse<T>(StoredProcedureExecuteResponse<T> storedProcedureResponse);

        Task ProcessResponse(string sessionToken, double responseRequestCharge, HttpStatusCode? statusCode);
    }
}
