// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using Microsoft.Health.CosmosDb.Features.Queries;
using Microsoft.Health.CosmosDb.Features.Storage;
using Microsoft.Health.Fhir.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Features.Persistence;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Storage
{
    /// <summary>
    /// A <see cref="Container"/> wrapper that, for each request:
    /// (1) sets the consistency level and, if applicable, the session token
    /// (2) Sets <see cref="FeedOptions.ResponseContinuationTokenLimitInKb"/>
    /// (3) Sets the <see cref="CosmosDbHeaders.RequestCharge"/> response header.
    /// (4) In the event of a 429 response from the database, throws a <see cref="RequestRateTooLargeException"/>.
    /// </summary>
    public class FhirContainer : Container
    {
        private static readonly string ValidConsistencyLevelsForErrorMessage = string.Join(", ", Enum.GetNames(typeof(ConsistencyLevel)).Select(v => $"'{v}'"));
        private readonly CosmosClient _client;
        private readonly Container _inner;
        private readonly IFhirRequestContextAccessor _fhirRequestContextAccessor;
        private readonly int? _continuationTokenSizeLimitInKb;
        private readonly ICosmosResponseProcessor _cosmosResponseProcessor;

        public FhirContainer(
            CosmosClient client,
            Container inner,
            IFhirRequestContextAccessor fhirRequestContextAccessor,
            int? continuationTokenSizeLimitInKb,
            ICosmosResponseProcessor cosmosResponseProcessor)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(inner, nameof(inner));
            EnsureArg.IsNotNull(fhirRequestContextAccessor, nameof(fhirRequestContextAccessor));
            EnsureArg.IsNotNull(continuationTokenSizeLimitInKb, nameof(continuationTokenSizeLimitInKb));
            EnsureArg.IsNotNull(cosmosResponseProcessor, nameof(cosmosResponseProcessor));

            _client = client;
            _inner = inner;
            _fhirRequestContextAccessor = fhirRequestContextAccessor;
            _continuationTokenSizeLimitInKb = continuationTokenSizeLimitInKb;
            _cosmosResponseProcessor = cosmosResponseProcessor;
        }

        public override string Id => _inner.Id;

        public override Database Database => _inner.Database;

        public override Conflicts Conflicts => _inner.Conflicts;

        public override Scripts Scripts => _inner.Scripts;

        public override async Task<ContainerResponse> ReadContainerAsync(ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.ReadContainerAsync(requestOptions, cancellationToken);
        }

        public override async Task<ResponseMessage> ReadContainerStreamAsync(ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.ReadContainerStreamAsync(requestOptions, cancellationToken);
        }

        public override async Task<ContainerResponse> ReplaceContainerAsync(ContainerProperties containerProperties, ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.ReplaceContainerAsync(containerProperties, requestOptions);
        }

        public override async Task<ResponseMessage> ReplaceContainerStreamAsync(ContainerProperties containerProperties, ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.ReplaceContainerStreamAsync(containerProperties, requestOptions);
        }

        public override async Task<ContainerResponse> DeleteContainerAsync(ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.DeleteContainerAsync(requestOptions, cancellationToken);
        }

        public override async Task<ResponseMessage> DeleteContainerStreamAsync(ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.DeleteContainerStreamAsync(requestOptions, cancellationToken);
        }

        public override async Task<int?> ReadThroughputAsync(CancellationToken cancellationToken = default)
        {
            return await _inner.ReadThroughputAsync(cancellationToken);
        }

        public override async Task<ThroughputResponse> ReadThroughputAsync(RequestOptions requestOptions, CancellationToken cancellationToken = default)
        {
            return await _inner.ReadThroughputAsync(requestOptions, cancellationToken);
        }

        public override async Task<ThroughputResponse> ReplaceThroughputAsync(int throughput, RequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.ReplaceThroughputAsync(throughput, requestOptions, cancellationToken);
        }

        public override async Task<ThroughputResponse> ReplaceThroughputAsync(ThroughputProperties throughputProperties, RequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.ReplaceThroughputAsync(throughputProperties, requestOptions, cancellationToken);
        }

        public override async Task<ResponseMessage> CreateItemStreamAsync(Stream streamPayload, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.CreateItemStreamAsync(streamPayload, partitionKey, UpdateOptions(requestOptions), cancellationToken);
        }

        public override async Task<ItemResponse<T>> CreateItemAsync<T>(T item, PartitionKey? partitionKey = null, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessResponse(await _inner.CreateItemAsync(item, partitionKey, UpdateOptions(requestOptions), cancellationToken));
        }

        public override async Task<ResponseMessage> ReadItemStreamAsync(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.ReadItemStreamAsync(id, partitionKey, UpdateOptions(requestOptions), cancellationToken);
        }

        public override async Task<ItemResponse<T>> ReadItemAsync<T>(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessResponse(await _inner.ReadItemAsync<T>(id, partitionKey, UpdateOptions(requestOptions), cancellationToken));
        }

        public override async Task<ResponseMessage> UpsertItemStreamAsync(Stream streamPayload, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.UpsertItemStreamAsync(streamPayload, partitionKey, UpdateOptions(requestOptions), cancellationToken);
        }

        public override async Task<ItemResponse<T>> UpsertItemAsync<T>(T item, PartitionKey? partitionKey = null, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessResponse(await _inner.UpsertItemAsync<T>(item, partitionKey, UpdateOptions(requestOptions)));
        }

        public override async Task<ResponseMessage> ReplaceItemStreamAsync(Stream streamPayload, string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.ReplaceItemStreamAsync(streamPayload, id, partitionKey, UpdateOptions(requestOptions), cancellationToken);
        }

        public override async Task<ItemResponse<T>> ReplaceItemAsync<T>(T item, string id, PartitionKey? partitionKey = null, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ProcessResponse(await _inner.ReplaceItemAsync<T>(item, id, partitionKey, UpdateOptions(requestOptions), cancellationToken));
        }

        public override async Task<ResponseMessage> DeleteItemStreamAsync(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.DeleteItemStreamAsync(id, partitionKey, UpdateOptions(requestOptions), cancellationToken);
        }

        public override async Task<ItemResponse<T>> DeleteItemAsync<T>(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await _inner.DeleteItemAsync<T>(id, partitionKey, UpdateOptions(requestOptions), cancellationToken);
        }

        public override FeedIterator GetItemQueryStreamIterator(QueryDefinition queryDefinition, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            return _inner.GetItemQueryStreamIterator(queryDefinition, continuationToken, UpdateOptions(requestOptions));
        }

        public override FeedIterator<T> GetItemQueryIterator<T>(QueryDefinition queryDefinition, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            return _inner.GetItemQueryIterator<T>(queryDefinition, continuationToken, UpdateOptions(requestOptions));
        }

        public override FeedIterator GetItemQueryStreamIterator(string queryText = null, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            return _inner.GetItemQueryStreamIterator(queryText, continuationToken, UpdateOptions(requestOptions));
        }

        public override FeedIterator<T> GetItemQueryIterator<T>(string queryText = null, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            return _inner.GetItemQueryIterator<T>(queryText, continuationToken, UpdateOptions(requestOptions));
        }

        public override IOrderedQueryable<T> GetItemLinqQueryable<T>(bool allowSynchronousQueryExecution = false, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            return _inner.GetItemLinqQueryable<T>(allowSynchronousQueryExecution, continuationToken, UpdateOptions(requestOptions));
        }

        public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilder<T>(string processorName, ChangesHandler<T> onChangesDelegate)
        {
            return _inner.GetChangeFeedProcessorBuilder<T>(processorName, onChangesDelegate);
        }

        public override ChangeFeedProcessorBuilder GetChangeFeedEstimatorBuilder(string processorName, ChangesEstimationHandler estimationDelegate, TimeSpan? estimationPeriod = null)
        {
            return _inner.GetChangeFeedEstimatorBuilder(processorName, estimationDelegate, estimationPeriod);
        }

        public override TransactionalBatch CreateTransactionalBatch(PartitionKey partitionKey)
        {
            return _inner.CreateTransactionalBatch(partitionKey);
        }

        private QueryRequestOptions UpdateOptions(QueryRequestOptions options)
        {
            (ConsistencyLevel? consistencyLevel, string sessionToken) = GetConsistencyHeaders();

            if (consistencyLevel == null && string.IsNullOrEmpty(sessionToken))
            {
                return options;
            }

            if (options == null)
            {
                options = new QueryRequestOptions();
            }

            if (consistencyLevel != null)
            {
                options.ConsistencyLevel = consistencyLevel;
            }

            if (_continuationTokenSizeLimitInKb != null)
            {
                options.ResponseContinuationTokenLimitInKb = _continuationTokenSizeLimitInKb;
            }

            if (!string.IsNullOrEmpty(sessionToken))
            {
                options.SessionToken = sessionToken;
            }

            return options;
        }

        private ItemRequestOptions UpdateOptions(ItemRequestOptions options)
        {
            (ConsistencyLevel? consistencyLevel, string sessionToken) = GetConsistencyHeaders();

            if (_continuationTokenSizeLimitInKb == null && consistencyLevel == null && string.IsNullOrEmpty(sessionToken))
            {
                return options;
            }

            if (options == null)
            {
                options = new ItemRequestOptions();
            }

            if (consistencyLevel != null)
            {
                options.ConsistencyLevel = consistencyLevel;
            }

            if (!string.IsNullOrEmpty(sessionToken))
            {
                options.SessionToken = sessionToken;
            }

            return options;
        }

        private (ConsistencyLevel? consistencyLevel, string sessionToken) GetConsistencyHeaders()
        {
            IFhirRequestContext fhirRequestContext = _fhirRequestContextAccessor.FhirRequestContext;

            if (fhirRequestContext == null)
            {
                return (null, null);
            }

            ConsistencyLevel? requestedConsistencyLevel = null;

            if (fhirRequestContext.RequestHeaders.TryGetValue(CosmosDbHeaders.ConsistencyLevel, out var values))
            {
                if (!Enum.TryParse(values, out ConsistencyLevel parsedLevel))
                {
                    throw new BadRequestException(string.Format(CultureInfo.CurrentCulture, Resources.UnrecognizedConsistencyLevel, values, ValidConsistencyLevelsForErrorMessage));
                }

                if (parsedLevel != _client.ClientOptions.ConsistencyLevel)
                {
                    if (!ValidateConsistencyLevel(parsedLevel))
                    {
                        throw new BadRequestException(string.Format(Resources.InvalidConsistencyLevel, parsedLevel, _client.ClientOptions.ConsistencyLevel));
                    }

                    requestedConsistencyLevel = parsedLevel;
                }
            }

            fhirRequestContext.RequestHeaders.TryGetValue(CosmosDbHeaders.SessionToken, out values);

            return (requestedConsistencyLevel, values);
        }

        /// <summary>
        /// Determines whether the requested consistency level is valid given the DocumentClient's consistency level.
        /// DocumentClient throws an ArgumentException when a requested consistency level is invalid. Since ArgumentException
        /// is not very specific and we would rather not inspect the exception message, we do the check ourselves here.
        /// Copied from the DocumentDB SDK.
        /// </summary>
        private bool ValidateConsistencyLevel(ConsistencyLevel desiredConsistency)
        {
            switch (_client.ClientOptions.ConsistencyLevel)
            {
                case ConsistencyLevel.Strong:
                    return desiredConsistency == ConsistencyLevel.Strong || desiredConsistency == ConsistencyLevel.BoundedStaleness || desiredConsistency == ConsistencyLevel.Session || desiredConsistency == ConsistencyLevel.Eventual || desiredConsistency == ConsistencyLevel.ConsistentPrefix;
                case ConsistencyLevel.BoundedStaleness:
                    return desiredConsistency == ConsistencyLevel.BoundedStaleness || desiredConsistency == ConsistencyLevel.Session || desiredConsistency == ConsistencyLevel.Eventual || desiredConsistency == ConsistencyLevel.ConsistentPrefix;
                case ConsistencyLevel.Session:
                case ConsistencyLevel.Eventual:
                case ConsistencyLevel.ConsistentPrefix:
                    return desiredConsistency == ConsistencyLevel.Session || desiredConsistency == ConsistencyLevel.Eventual || desiredConsistency == ConsistencyLevel.ConsistentPrefix;
                default:
                    throw new NotSupportedException(nameof(_client.ClientOptions.ConsistencyLevel));
            }
        }

        private async Task<ItemResponse<T>> ProcessResponse<T>(ItemResponse<T> response)
        {
            await _cosmosResponseProcessor.ProcessResponse(response);
            return response;
        }

        private async Task<FeedResponse<T>> ProcessResponse<T>(FeedResponse<T> response)
        {
            await _cosmosResponseProcessor.ProcessResponse(response);
            return response;
        }

        private async Task<StoredProcedureExecuteResponse<T>> ProcessResponse<T>(StoredProcedureExecuteResponse<T> response)
        {
            await _cosmosResponseProcessor.ProcessResponse(response);
            return response;
        }

        private async Task ProcessException(Exception ex)
        {
            await _cosmosResponseProcessor.ProcessException(ex);
        }
    }
}
