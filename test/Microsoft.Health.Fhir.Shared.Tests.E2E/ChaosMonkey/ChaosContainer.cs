// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;

namespace Microsoft.Health.Fhir.Tests.E2E.ChaosMonkey
{
    /// <summary>
    /// A container for testing that behaves more like the real world.
    /// Your local development will never be the same again.
    /// </summary>
    [SuppressMessage("ReSharper", "SA1129", Justification = "Test class")]
    public class ChaosContainer : Container
    {
        private readonly Container _inner;
        private readonly Random _random = new Random();

        public ChaosContainer(Container inner)
        {
            _inner = inner;
        }

        public override string Id => _inner.Id;

        public override Database Database => _inner.Database;

        public override Conflicts Conflicts => _inner.Conflicts;

        public override Scripts Scripts => _inner.Scripts;

        public override Task<ContainerResponse> ReadContainerAsync(ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = new())
        {
            ShouldThrowWrench();

            return _inner.ReadContainerAsync(requestOptions, cancellationToken);
        }

        public override Task<ResponseMessage> ReadContainerStreamAsync(ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReadContainerStreamAsync(requestOptions, cancellationToken);
        }

        public override Task<ContainerResponse> ReplaceContainerAsync(ContainerProperties containerProperties, ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReplaceContainerAsync(containerProperties, requestOptions, cancellationToken);
        }

        public override Task<ResponseMessage> ReplaceContainerStreamAsync(ContainerProperties containerProperties, ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReplaceContainerStreamAsync(containerProperties, requestOptions, cancellationToken);
        }

        public override Task<ContainerResponse> DeleteContainerAsync(ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.DeleteContainerAsync(requestOptions, cancellationToken);
        }

        public override Task<ResponseMessage> DeleteContainerStreamAsync(ContainerRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.DeleteContainerStreamAsync(requestOptions, cancellationToken);
        }

        public override Task<int?> ReadThroughputAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReadThroughputAsync(cancellationToken);
        }

        public override Task<ThroughputResponse> ReadThroughputAsync(RequestOptions requestOptions, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReadThroughputAsync(requestOptions, cancellationToken);
        }

        public override Task<ThroughputResponse> ReplaceThroughputAsync(int throughput, RequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReplaceThroughputAsync(throughput, requestOptions, cancellationToken);
        }

        public override Task<ThroughputResponse> ReplaceThroughputAsync(ThroughputProperties throughputProperties, RequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReplaceThroughputAsync(throughputProperties, requestOptions, cancellationToken);
        }

        public override Task<ResponseMessage> CreateItemStreamAsync(Stream streamPayload, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.CreateItemStreamAsync(streamPayload, partitionKey, requestOptions, cancellationToken);
        }

        public override Task<ItemResponse<T>> CreateItemAsync<T>(T item, PartitionKey? partitionKey = null, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.CreateItemAsync(item, partitionKey, requestOptions, cancellationToken);
        }

        public override Task<ResponseMessage> ReadItemStreamAsync(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReadItemStreamAsync(id, partitionKey, requestOptions, cancellationToken);
        }

        public override Task<ItemResponse<T>> ReadItemAsync<T>(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReadItemAsync<T>(id, partitionKey, requestOptions, cancellationToken);
        }

        public override Task<ResponseMessage> UpsertItemStreamAsync(Stream streamPayload, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.UpsertItemStreamAsync(streamPayload, partitionKey, requestOptions, cancellationToken);
        }

        public override Task<ItemResponse<T>> UpsertItemAsync<T>(T item, PartitionKey? partitionKey = null, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.UpsertItemAsync(item, partitionKey, requestOptions, cancellationToken);
        }

        public override Task<ResponseMessage> ReplaceItemStreamAsync(Stream streamPayload, string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReplaceItemStreamAsync(streamPayload, id, partitionKey, requestOptions, cancellationToken);
        }

        public override Task<ItemResponse<T>> ReplaceItemAsync<T>(T item, string id, PartitionKey? partitionKey = null, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReplaceItemAsync(item, id, partitionKey, requestOptions, cancellationToken);
        }

        public override Task<ResponseMessage> ReadManyItemsStreamAsync(IReadOnlyList<(string id, PartitionKey partitionKey)> items, ReadManyRequestOptions readManyRequestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReadManyItemsStreamAsync(items, readManyRequestOptions, cancellationToken);
        }

        public override Task<FeedResponse<T>> ReadManyItemsAsync<T>(IReadOnlyList<(string id, PartitionKey partitionKey)> items, ReadManyRequestOptions readManyRequestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.ReadManyItemsAsync<T>(items, readManyRequestOptions, cancellationToken);
        }

        public override Task<ResponseMessage> DeleteItemStreamAsync(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.DeleteItemStreamAsync(id, partitionKey, requestOptions, cancellationToken);
        }

        public override Task<ItemResponse<T>> DeleteItemAsync<T>(string id, PartitionKey partitionKey, ItemRequestOptions requestOptions = null, CancellationToken cancellationToken = new CancellationToken())
        {
            ShouldThrowWrench();

            return _inner.DeleteItemAsync<T>(id, partitionKey, requestOptions, cancellationToken);
        }

        public override FeedIterator GetItemQueryStreamIterator(QueryDefinition queryDefinition, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            ShouldThrowWrench();

            return _inner.GetItemQueryStreamIterator(queryDefinition, continuationToken, requestOptions);
        }

        public override FeedIterator<T> GetItemQueryIterator<T>(QueryDefinition queryDefinition, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            ShouldThrowWrench();

            return _inner.GetItemQueryIterator<T>(queryDefinition, continuationToken, requestOptions);
        }

        public override FeedIterator GetItemQueryStreamIterator(string queryText = null, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            ShouldThrowWrench();

            return _inner.GetItemQueryStreamIterator(queryText, continuationToken, requestOptions);
        }

        public override FeedIterator<T> GetItemQueryIterator<T>(string queryText = null, string continuationToken = null, QueryRequestOptions requestOptions = null)
        {
            ShouldThrowWrench();

            return _inner.GetItemQueryIterator<T>(queryText, continuationToken, requestOptions);
        }

        public override IOrderedQueryable<T> GetItemLinqQueryable<T>(bool allowSynchronousQueryExecution = false, string continuationToken = null, QueryRequestOptions requestOptions = null, CosmosLinqSerializerOptions linqSerializerOptions = null)
        {
            ShouldThrowWrench();

            return _inner.GetItemLinqQueryable<T>(allowSynchronousQueryExecution, continuationToken, requestOptions, linqSerializerOptions);
        }

        public override ChangeFeedProcessorBuilder GetChangeFeedProcessorBuilder<T>(string processorName, ChangesHandler<T> onChangesDelegate)
        {
            ShouldThrowWrench();

            return _inner.GetChangeFeedProcessorBuilder(processorName, onChangesDelegate);
        }

        public override ChangeFeedProcessorBuilder GetChangeFeedEstimatorBuilder(string processorName, ChangesEstimationHandler estimationDelegate, TimeSpan? estimationPeriod = null)
        {
            ShouldThrowWrench();

            return _inner.GetChangeFeedEstimatorBuilder(processorName, estimationDelegate, estimationPeriod);
        }

        public override ChangeFeedEstimator GetChangeFeedEstimator(string processorName, Container leaseContainer)
        {
            ShouldThrowWrench();

            return _inner.GetChangeFeedEstimator(processorName, leaseContainer);
        }

        public override TransactionalBatch CreateTransactionalBatch(PartitionKey partitionKey)
        {
            ShouldThrowWrench();

            return _inner.CreateTransactionalBatch(partitionKey);
        }

        private void ShouldThrowWrench()
        {
            if (_random.Next(3) == 2)
            {
                throw new CosmosException("ChaosContainer: Request rate exceeded", HttpStatusCode.TooManyRequests, 0, Activity.Current?.Id, 1);
            }

            if (_random.Next(10) == 5)
            {
                throw new CosmosException("ChaosContainer: Service Unavailable", HttpStatusCode.ServiceUnavailable, 0, Activity.Current?.Id, 1);
            }
        }
    }
}
