// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Routing;
using Microsoft.Health.Fhir.Core.Features.Search;
using NSubstitute;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.UnitTests.Features.Search
{
    public class SearchServiceTests
    {
        private const string ParamNameSearchOptionsFactory = "searchOptionsFactory";
        private static readonly Uri SearchUrl = new Uri("http://test");

        private readonly IUrlResolver _urlResolver = Substitute.For<IUrlResolver>();
        private readonly ISearchOptionsFactory _searchOptionsFactory = Substitute.For<ISearchOptionsFactory>();
        private readonly IFhirRequestContextAccessor _fhirRequestContextAccessor = Substitute.For<IFhirRequestContextAccessor>();

        private readonly TestSearchService _searchService;
        private readonly RawResourceFactory _rawResourceFactory;
        private readonly ResourceRequest _resourceRequest = new ResourceRequest("http://fhir", HttpMethod.Post);
        private readonly string _correlationId;
        private readonly IFhirDataStore _fhirDataStore;

        public SearchServiceTests()
        {
            _fhirDataStore = Substitute.For<IFhirDataStore>();

            _searchOptionsFactory.Create(Arg.Any<string>(), Arg.Any<IReadOnlyList<Tuple<string, string>>>())
                .Returns(x => new SearchOptions());

            _searchService = new TestSearchService(_searchOptionsFactory, _fhirDataStore);
            _rawResourceFactory = new RawResourceFactory(new FhirJsonSerializer());

            _urlResolver.ResolveRouteUrl(Arg.Any<IEnumerable<Tuple<string, string>>>()).Returns(SearchUrl);

            _correlationId = Guid.NewGuid().ToString();
            _fhirRequestContextAccessor.FhirRequestContext.CorrelationId.Returns(_correlationId);
        }

        private class TestSearchService : SearchService
        {
            public TestSearchService(ISearchOptionsFactory searchOptionsFactory, IFhirDataStore fhirDataStore)
                : base(searchOptionsFactory, fhirDataStore)
            {
                SearchImplementation = options => null;
            }

            public Func<SearchOptions, SearchResult> SearchImplementation { get; set; }

            protected override Task<SearchResult> SearchInternalAsync(
                SearchOptions searchOptions,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(SearchImplementation(searchOptions));
            }

            protected override Task<SearchResult> SearchHistoryInternalAsync(
                SearchOptions searchOptions,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(SearchImplementation(searchOptions));
            }
        }
    }
}
