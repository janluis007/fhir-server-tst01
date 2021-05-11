// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Features.Definition;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Features.Search.Expressions;
using Microsoft.Health.Fhir.Core.Features.Search.Expressions.Parsers;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Everything
{
    public class PatientEverythingService : IPatientEverythingService
    {
        private readonly Func<IScoped<ISearchService>> _searchServiceFactory;
        private readonly ISearchOptionsFactory _searchOptionsFactory;
        private readonly IExpressionParser _expressionParser;
        private readonly ISearchParameterDefinitionManager _searchParameterDefinitionManager;
        private const int _everythingMaxSubqueryItemLimit = 100;

        public PatientEverythingService(
            Func<IScoped<ISearchService>> searchServiceFactory,
            ISearchOptionsFactory searchOptionsFactory,
            IExpressionParser expressionParser,
            ISearchParameterDefinitionManager.SearchableSearchParameterDefinitionManagerResolver searchParameterDefinitionManagerResolver)
        {
            EnsureArg.IsNotNull(searchServiceFactory, nameof(searchServiceFactory));
            EnsureArg.IsNotNull(searchOptionsFactory, nameof(searchOptionsFactory));
            EnsureArg.IsNotNull(expressionParser, nameof(expressionParser));
            EnsureArg.IsNotNull(searchParameterDefinitionManagerResolver, nameof(searchParameterDefinitionManagerResolver));

            _searchServiceFactory = searchServiceFactory;
            _searchOptionsFactory = searchOptionsFactory;
            _expressionParser = expressionParser;
            _searchParameterDefinitionManager = searchParameterDefinitionManagerResolver();
        }

        public async Task<SearchResult> SearchAsync(
            string resourceType,
            string resourceId,
            PartialDateTime start,
            PartialDateTime end,
            PartialDateTime since,
            string type,
            int? count,
            string continuationToken,
            IReadOnlyList<string> includes,
            IReadOnlyList<Tuple<string, string>> revincludes,
            CancellationToken cancellationToken)
        {
            using IScoped<ISearchService> search = _searchServiceFactory();

            if (string.Equals(search.Value.GetType().Name, "SqlServerSearchService", StringComparison.Ordinal))
            {
                throw new OperationNotImplementedException("$everything operation is not yet implemented in SQL Server.");
            }

            // If continuation token provided, we are in second page or after, return compartment search results
            SearchOptions searchOptions;
            if (!string.IsNullOrEmpty(continuationToken))
            {
                searchOptions = CreateSearchOptions(resourceType, resourceId, start, end, since, type, count, continuationToken);
                return await search.Value.SearchAsync(searchOptions, cancellationToken);
            }

            // Otherwise we are in first page, return resource, include, revinclude and first compartment search result
            var searchResultEntries = new List<SearchResultEntry>();
            SearchResult searchResult = await SearchReferencesForEverythingOperation(resourceType, resourceId, since, type, includes, revincludes, cancellationToken);
            searchResultEntries.AddRange(searchResult.Results);

            searchOptions = CreateSearchOptions(resourceType, resourceId, start, end, since, type, 1, continuationToken);
            searchResult = await search.Value.SearchAsync(searchOptions, cancellationToken);
            searchResultEntries.AddRange(searchResult.Results);

            return new SearchResult(searchResultEntries, searchResult.ContinuationToken, null, new List<Tuple<string, string>>());
        }

        private async Task<SearchResult> SearchReferencesForEverythingOperation(
            string resourceType,
            string resourceId,
            PartialDateTime since,
            string type,
            IReadOnlyList<string> includes,
            IReadOnlyList<Tuple<string, string>> revincludes,
            CancellationToken cancellationToken)
        {
            using IScoped<ISearchService> search = _searchServiceFactory();
            var searchResultEntries = new List<SearchResultEntry>();

            // Build search parameters
            var searchParameters = new List<Tuple<string, string>>
            {
                Tuple.Create(SearchParameterNames.Id, resourceId),
            };

            searchParameters.AddRange(includes.Select(include => Tuple.Create(SearchParameterNames.Include, $"{resourceType}:{include}")));

            // Search with includes
            SearchOptions searchOptions = _searchOptionsFactory.Create(resourceType, searchParameters);
            SearchResult searchResult = await search.Value.SearchAsync(searchOptions, cancellationToken);
            searchResultEntries.AddRange(searchResult.Results.Select(x => new SearchResultEntry(x.Resource)));

            // Search with revincludes
            // Currently we only have one revinclude resource. If we are going to pull more revinclude resources, we will need to refine this to get better performance.
            // We do not use _revinclude here since _revinclude depends on the existence of the parent resource.
            foreach (Tuple<string, string> revinclude in revincludes)
            {
                searchParameters = new List<Tuple<string, string>>
                {
                    Tuple.Create(revinclude.Item2, resourceId),
                    Tuple.Create(KnownQueryParameterNames.Count, _everythingMaxSubqueryItemLimit.ToString()),
                };

                searchOptions = _searchOptionsFactory.Create(revinclude.Item1, searchParameters);
                searchResult = await search.Value.SearchAsync(searchOptions, cancellationToken);
                searchResultEntries.AddRange(searchResult.Results);
            }

            // Filter results by IsDeleted
            searchResultEntries = searchResultEntries.Where(e => !e.Resource.IsDeleted).ToList();

            // Filter results by _type
            if (!string.IsNullOrEmpty(type))
            {
                IReadOnlyList<string> types = type.SplitByOrSeparator();
                searchResultEntries = searchResultEntries.Where(s => types.Contains(s.Resource.ResourceTypeName)).ToList();
            }

            // Filter results by _since
            if (since != null)
            {
                var sinceDateTimeOffset = since.ToDateTimeOffset(
                    defaultMonth: 1,
                    defaultDaySelector: (year, month) => 1,
                    defaultHour: 0,
                    defaultMinute: 0,
                    defaultSecond: 0,
                    defaultFraction: 0.0000000m,
                    defaultUtcOffset: TimeSpan.Zero);
                searchResultEntries = searchResultEntries.Where(s => s.Resource.LastModified.CompareTo(sinceDateTimeOffset) >= 0).ToList();
            }

            return new SearchResult(searchResultEntries, searchResult.ContinuationToken, searchResult.SortOrder, searchResult.UnsupportedSearchParameters);
        }

        private SearchOptions CreateSearchOptions(string compartmentType, string compartmentId, PartialDateTime start, PartialDateTime end, PartialDateTime since, string type, int? count, string continuationToken)
        {
            var queryParameters = new List<Tuple<string, string>>();

            if (since != null)
            {
                queryParameters.Add(Tuple.Create(SearchParameterNames.LastUpdated, $"ge{since}"));
            }

            if (!string.IsNullOrEmpty(type))
            {
                queryParameters.Add(Tuple.Create(SearchParameterNames.ResourceType, type));
            }

            if (count > 0)
            {
                queryParameters.Add(Tuple.Create(KnownQueryParameterNames.Count, count.ToString()));
            }

            if (!string.IsNullOrEmpty(continuationToken))
            {
                queryParameters.Add(Tuple.Create(KnownQueryParameterNames.ContinuationToken, continuationToken));
            }

            SearchOptions searchOptions = _searchOptionsFactory.Create(compartmentType, compartmentId, null, queryParameters);

            // Return if start and end not specified
            if (start == null && end == null)
            {
                return searchOptions;
            }

            // Otherwise rewrite expression with start and end
            SearchParameterInfo clinicalDateInfo = _searchParameterDefinitionManager.GetSearchParameter(SearchParameterNames.ClinicalDateUri);
            var resourceTypeString = new[] { clinicalDateInfo.BaseResourceTypes.ToList().First() };

            // Add expression for compartment search
            var expressions = new List<Expression>
            {
                Expression.CompartmentSearch(compartmentType, compartmentId),
            };

            // Add expression for _since
            if (since != null)
            {
                expressions.Add(_expressionParser.Parse(resourceTypeString, SearchParameterNames.LastUpdated, $"ge{since}"));
            }

            // Add expression for date and _type
            // The expression looks like:
            // Expression.Or(
            //     Expression.And(SearchParameterExpression(_type: the intersection of _type and 17 resource types that have clinical date), SearchParameterExpression(date: "ge{start}/le{end}")),
            //     SearchParameterExpression(_type: the intersection of _type and other resource types that do not have clinical date)
            // )
            Expression dateExpression = null;
            List<string> dateResourceTypes = type == null
                ? clinicalDateInfo.BaseResourceTypes.ToList()
                : clinicalDateInfo.BaseResourceTypes.Intersect(type.SplitByOrSeparator()).ToList();

            if (dateResourceTypes.Any())
            {
                var dateExpressions = new List<Expression>
                {
                    _expressionParser.Parse(resourceTypeString, SearchParameterNames.ResourceType, string.Join(',', dateResourceTypes)),
                };

                if (start != null)
                {
                    dateExpressions.Add(_expressionParser.Parse(resourceTypeString, SearchParameterNames.Date, $"ge{start}"));
                }

                if (end != null)
                {
                    dateExpressions.Add(_expressionParser.Parse(resourceTypeString, SearchParameterNames.Date, $"le{end}"));
                }

                dateExpression = Expression.And(dateExpressions);
            }

            Expression nonDateExpression = null;
            List<string> nonDateResourceTypes = type == null
                ? Enum.GetNames(typeof(Hl7.Fhir.Model.ResourceType)).Except(clinicalDateInfo.BaseResourceTypes).ToList()
                : Enum.GetNames(typeof(Hl7.Fhir.Model.ResourceType)).Except(clinicalDateInfo.BaseResourceTypes).Intersect(type.SplitByOrSeparator()).ToList();

            if (nonDateResourceTypes.Any())
            {
                nonDateExpression = _expressionParser.Parse(resourceTypeString, SearchParameterNames.ResourceType, string.Join(',', nonDateResourceTypes));
            }

            if (dateExpression != null && nonDateExpression != null)
            {
                expressions.Add(Expression.Or(dateExpression, nonDateExpression));
            }
            else if (dateExpression != null)
            {
                expressions.Add(dateExpression);
            }
            else if (nonDateExpression != null)
            {
                expressions.Add(nonDateExpression);
            }
            else
            {
                // If both of them are null, it means _type is invalid. Just put it here to return nothing.
                expressions.Add(_expressionParser.Parse(resourceTypeString, SearchParameterNames.ResourceType, type));
            }

            searchOptions.Expression = expressions.Count > 1 ? Expression.And(expressions) : expressions[0];

            return searchOptions;
        }
    }
}
