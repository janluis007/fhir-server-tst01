// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Features;
using Microsoft.Health.Fhir.Core.Features.Definition;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.CosmosDb.Configs;
using Microsoft.Health.Fhir.CosmosDb.Features.Storage;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Migrate
{
    public class CosmosDataExporter : IMigrateDataExporter
    {
        private readonly IScoped<Container> _containerScope;
        private readonly CosmosDataStoreConfiguration _cosmosDataStoreConfiguration;
        private readonly ISearchParameterDefinitionManager _searchParameterDefinitionManager;

        public CosmosDataExporter(
            IScoped<Container> containerScope,
            ISearchParameterDefinitionManager searchParameterDefinitionManager,
            CosmosDataStoreConfiguration cosmosDataStoreConfiguration)
        {
            _containerScope = containerScope;
            _searchParameterDefinitionManager = searchParameterDefinitionManager;
            _cosmosDataStoreConfiguration = cosmosDataStoreConfiguration;
        }

        public async IAsyncEnumerable<List<ResourceWrapper>> Export()
        {
            var sqlQueryText = "SELECT * FROM c WHERE c.isSystem = false";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);

            var queryOptions = new QueryRequestOptions
            {
                MaxConcurrency = 20,
                MaxItemCount = 20000,
                MaxBufferedItemCount = 100000,
            };
            FeedIterator<FhirCosmosResourceWrapper> queryResultSetIterator = _containerScope.Value.GetItemQueryIterator<FhirCosmosResourceWrapper>(queryDefinition, null, queryOptions);

            while (queryResultSetIterator.HasMoreResults)
            {
                List<ResourceWrapper> entities = new List<ResourceWrapper>();

                FeedResponse<FhirCosmosResourceWrapper> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (FhirCosmosResourceWrapper entity in currentResultSet)
                {
                    foreach (var searchIndexEntry in entity.SearchIndices)
                    {
                        searchIndexEntry.SearchParameter = _searchParameterDefinitionManager.GetSearchParameter(entity.ResourceTypeName, searchIndexEntry.SearchParameter.Name);
                    }

                    entities.Add(entity);
                }

                yield return entities;
            }
        }
    }
}
