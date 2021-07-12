// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.CosmosDb.Configs;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Storage
{
    public class CosmosClientReadWriteTestProvider : ICosmosClientTestProvider
    {
        private readonly ILogger<CosmosClientReadWriteTestProvider> _logger;
        private readonly HealthCheckDocument _document;
        private readonly PartitionKey _partitionKey;

        public CosmosClientReadWriteTestProvider(ILogger<CosmosClientReadWriteTestProvider> logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));

            _logger = logger;
            _document = new HealthCheckDocument();
            _partitionKey = new PartitionKey(_document.PartitionKey);
        }

        public async Task PerformTest(Container container, CosmosDataStoreConfiguration configuration, CosmosCollectionConfiguration cosmosCollectionConfiguration)
        {
            _logger.LogDebug("Performing healthcheck for {HealthCheckDocumentId}", _document.Id);

            var requestOptions = new ItemRequestOptions { ConsistencyLevel = ConsistencyLevel.Session };

            var resourceResponse = await container.UpsertItemAsync(
                _document,
                _partitionKey,
                requestOptions);

            requestOptions.SessionToken = resourceResponse.Headers.Session;

            await container.ReadItemAsync<HealthCheckDocument>(resourceResponse.Resource.Id, _partitionKey, requestOptions);
        }

        private class HealthCheckDocument : SystemData
        {
            public HealthCheckDocument()
            {
                var guid = Guid.NewGuid();
                Id = guid;
                PartitionKey = $"__healthcheck{guid}__";
            }

            [JsonProperty(KnownDocumentProperties.PartitionKey)]
            public string PartitionKey { get; };

            [JsonProperty("_ttl")]
            public int Ttl { get; } = 10;
        }
    }
}
