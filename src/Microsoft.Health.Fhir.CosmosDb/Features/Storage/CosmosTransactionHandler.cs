// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Features.Persistence;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Storage
{
    internal class CosmosTransactionHandler : ITransactionHandler
    {
        private readonly IScoped<Container> _container;
        private readonly IScoped<IFhirDataStore> _dataStore;

        public CosmosTransactionHandler(IScoped<Container> container, IScoped<IFhirDataStore> dataStore)
        {
            _container = container;
            _dataStore = dataStore;
        }

        public ITransactionScope BeginTransaction()
        {
            var scope = new CosmosTransactionScope(_container.Value, (CosmosFhirDataStore)_dataStore.Value);
            return scope;
        }

        public void Dispose()
        {
        }

        public class CosmosTransactionScope : ITransactionScope, IBatchedTransaction
        {
            private readonly Container _container;
            private readonly CosmosFhirDataStore _fhirDataStore;
            private string _partitionKey = null;
            private TransactionalBatch _transactionalBatch;
            private readonly TaskCompletionSource<bool> _completion = new();
            private int _items = 0;

            public CosmosTransactionScope(Container container, CosmosFhirDataStore fhirDataStore)
            {
                _container = container;
                _fhirDataStore = fhirDataStore;
                fhirDataStore.CurrentTransactionScope = this;
            }

            public Task CompletionToken => _completion.Task;

            public TransactionalBatchResponse Results { get; set; }

            public void AddCreateOperation(FhirCosmosResourceWrapper resource)
            {
                CheckPartitionKeys(resource.PartitionKey);
                _transactionalBatch.CreateItem(resource);
                Interlocked.Increment(ref _items);
            }

            public void AddReadOperation(string resourcePartitionKey, string id)
            {
                CheckPartitionKeys(resourcePartitionKey);
                _transactionalBatch.ReadItem(id);
                Interlocked.Increment(ref _items);
            }

            public void AddReplaceOperation(FhirCosmosResourceWrapper resource)
            {
                CheckPartitionKeys(resource.PartitionKey);
                _transactionalBatch.ReplaceItem(resource.Id, resource);
                Interlocked.Increment(ref _items);
            }

            public void AddDeleteOperation(FhirCosmosResourceWrapper resource)
            {
                CheckPartitionKeys(resource.PartitionKey);
                _transactionalBatch.DeleteItem(resource.Id);
                Interlocked.Increment(ref _items);
            }

            public void Dispose()
            {
                _fhirDataStore.CurrentTransactionScope = null;
                GC.SuppressFinalize(this);
            }

            public void Complete()
            {
                Results = _transactionalBatch.ExecuteAsync().GetAwaiter().GetResult();
                _completion.SetResult(true);
            }

            public async Task CompleteAsync(int expectedItems)
            {
                while (expectedItems != _items)
                {
                    await Task.Delay(10);
                }

                Results = await _transactionalBatch.ExecuteAsync();
                _completion.SetResult(true);
            }

            private void CheckPartitionKeys(string resourcePartitionKey)
            {
                if (string.IsNullOrEmpty(_partitionKey))
                {
                    _partitionKey = resourcePartitionKey;
                    _transactionalBatch = _container.CreateTransactionalBatch(new PartitionKey(_partitionKey));
                }
                else if (!string.Equals(_partitionKey, resourcePartitionKey, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
