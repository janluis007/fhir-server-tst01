// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.TableStorage.Configs;
using Microsoft.Health.Fhir.TableStorage.Features.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.TableStorage.Features.Search
{
    public class TableStorageSearchService : SearchService
    {
        private readonly TableStorageDataStoreConfiguration _config;
        private CloudTable _table;

        public TableStorageSearchService(
            ISearchOptionsFactory searchOptionsFactory,
            IFhirDataStore fhirDataStore,
            TableStorageDataStoreConfiguration config,
            CloudTableClient client)
            : base(searchOptionsFactory, fhirDataStore)
        {
            _config = config;
            _table = client.GetTableReference(config.TableName);
            _table.CreateIfNotExistsAsync().GetAwaiter().GetResult();
        }

        protected override async Task<SearchResult> SearchInternalAsync(
            SearchOptions searchOptions,
            CancellationToken cancellationToken)
        {
            var filters = new StringBuilder();
            filters.Append(TableQuery.GenerateFilterConditionForBool(nameof(KnownResourceWrapperProperties.IsDeleted), QueryComparisons.Equal, false));

            var query = new TableQuery<FhirTableEntity>();

            var expressionQueryBuilder = new ExpressionQueryBuilder(filters, _config);

            if (searchOptions.Expression != null)
            {
                filters.Append(" and ");
                searchOptions.Expression.AcceptVisitor(expressionQueryBuilder, default);
            }

            Debug.WriteLine(filters);

            query.Where(filters.ToString());

            query = query.Take(searchOptions.MaxItemCount);

            if (searchOptions.CountOnly)
            {
                var count = await RowCount(query);

                return new SearchResult(count, searchOptions.UnsupportedSearchParams);
            }
            else
            {
                TableContinuationToken ct = GetContinuationToken(searchOptions);

                TableQuerySegment<FhirTableEntity> results =
                    await _table.ExecuteQuerySegmentedAsync(query, ct);

                while (_config.AllowTableScans && results.ContinuationToken != null && results.Results.Count == 0)
                {
                    // This query returns no results and a continuation token, likely means its doing
                    // a full table scan, which will be slow
                    results = await _table.ExecuteQuerySegmentedAsync(query, results.ContinuationToken);
                }

                var searchResults = new SearchResult(
                    results.Results.Select(TableStorageFhirDataStore.ToSearchResultEntry),
                    searchOptions.UnsupportedSearchParams,
                    searchOptions.UnsupportedSortingParams,
                    SerializeContinuationToken(results));

                if (searchOptions.IncludeTotal == TotalType.Accurate)
                {
                    searchResults.TotalCount = await RowCount(query);
                }

                return searchResults;
            }
        }

        protected override async Task<SearchResult> SearchHistoryInternalAsync(
            SearchOptions searchOptions,
            CancellationToken cancellationToken)
        {
            var filters = new StringBuilder();
            var query = new TableQuery<FhirTableEntity>();
            var expressionQueryBuilder = new ExpressionQueryBuilder(filters, _config);

            if (searchOptions.Expression != null)
            {
                searchOptions.Expression.AcceptVisitor(expressionQueryBuilder, new ExpressionQueryBuilder.Context { IsHistory = true });
            }

            Debug.WriteLine(filters);

            query.Where(filters.ToString());

            query = query.Take(searchOptions.MaxItemCount);

            TableContinuationToken ct = GetContinuationToken(searchOptions);

            TableQuerySegment<FhirTableEntity> results = await _table.ExecuteQuerySegmentedAsync(query, ct);

            while (_config.AllowTableScans && results.ContinuationToken != null && results.Results.Count == 0)
            {
                // This query returns no results and a continuation token, likely means its doing
                // a full table scan, which will be slow
                results = await _table.ExecuteQuerySegmentedAsync(query, results.ContinuationToken);
            }

            return new SearchResult(
                results.Results.Select(TableStorageFhirDataStore.ToSearchResultEntry),
                searchOptions.UnsupportedSearchParams,
                searchOptions.UnsupportedSortingParams,
                SerializeContinuationToken(results));
        }

        private static TableContinuationToken GetContinuationToken(SearchOptions searchOptions)
        {
            return string.IsNullOrEmpty(searchOptions.ContinuationToken) ?
                new TableContinuationToken() :
                JsonConvert.DeserializeObject<TableContinuationToken>(Encoding.UTF8.GetString(Convert.FromBase64String(searchOptions.ContinuationToken)));
        }

        private static string SerializeContinuationToken(TableQuerySegment<FhirTableEntity> results)
        {
            return results.ContinuationToken == null ? null : Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(results.ContinuationToken)));
        }

        public async Task<int> RowCount(TableQuery<FhirTableEntity> query)
        {
            query = query.Select(new List<string> { "PartitionKey", "RowKey" });

            int count = 0;
            TableContinuationToken token = null;
            do
            {
                var result = await _table.ExecuteQuerySegmentedAsync(query, token);
                token = result.ContinuationToken;
                count += result.Count();
            }
            while (token != null);

            return count;
        }
    }
}
