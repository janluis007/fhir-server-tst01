// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features;
using Microsoft.Health.Fhir.Core.Features.Operations.Export;
using Microsoft.Health.Fhir.Core.Features.Operations.Export.Models;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Messages.Export;
using Microsoft.Health.Fhir.Tests.Common;
using NSubstitute;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.UnitTests.Features.Operations.Export
{
    public class ExportExecutorTests
    {
        private int _maxItemCountPerQuery = 10;
        private ResourceDeserializer _deserializer;
        private ISearchService _searchService;
        private ExportJobConfiguration _exportJobConfiguration = new ExportJobConfiguration();

        private IExportExecutor _exportExecutor;

        private CreateExportRequest _exportRequest;
        private readonly RawResourceFactory _rawResourceFactory;
        private readonly ResourceRequest _resourceRequest = new ResourceRequest("http://fhir", HttpMethod.Post);

        public ExportExecutorTests()
        {
            _searchService = Substitute.For<ISearchService>();
            _deserializer = Deserializers.ResourceDeserializer;
            _exportJobConfiguration.MaxItemCountPerQuery = _maxItemCountPerQuery;

            _exportExecutor = new ExportExecutor(_searchService, _deserializer, Options.Create(_exportJobConfiguration));

            _exportRequest = new CreateExportRequest(new Uri("https://localhost:44348/$export"), "AzureBlockBlob", "destinationConnection");
            _rawResourceFactory = new RawResourceFactory(new FhirJsonSerializer());
    }

        [Fact]
        public async Task GivenMaxItemPerQueryCountIsSet_WhenCallingGetExportDataAsync_ThenSearchOptionsContainsMaxItemCount()
        {
            var searchResult = new SearchResult(new List<ResourceWrapper>(), continuationToken: null);
            _searchService.InternalRequestForSearchAsync(null, null, null).ReturnsForAnyArgs(searchResult);

            await _exportExecutor.GetExportDataAsync(_exportRequest, jobProgress: null);

            await _searchService.Received(1).InternalRequestForSearchAsync(
                Arg.Any<string>(),
                Arg.Any<List<Tuple<string, string>>>(),
                Arg.Is<SearchOptions>(x => x.MaxItemCount == _maxItemCountPerQuery));
        }

        [Fact]
        public async Task GivenJobProgressIsNotNull_WhenCallingGetExportDataAsync_ThenQueryParamsContainsContinuationToken()
        {
            string continuationToken = "continuationToken123";
            var jobProgress = new ExportJobProgress(continuationToken, 0);

            var searchResult = new SearchResult(new List<ResourceWrapper>(), continuationToken);
            _searchService.InternalRequestForSearchAsync(null, null, null).ReturnsForAnyArgs(searchResult);

            await _exportExecutor.GetExportDataAsync(_exportRequest, jobProgress);

            await _searchService.Received(1).InternalRequestForSearchAsync(
                Arg.Any<string>(),
                Arg.Is<List<Tuple<string, string>>>(x => x.Any(t => t.Item1 == KnownQueryParameterNames.ContinuationToken && t.Item2 == continuationToken)),
                Arg.Any<SearchOptions>());
        }

        [Fact]
        public async Task GivenJobProgressIsNull_WhenCallingGetExportDataAsync_ThenQueryParamsContainsContinuationToken()
        {
            var searchResult = new SearchResult(new List<ResourceWrapper>(), continuationToken: null);
            _searchService.InternalRequestForSearchAsync(null, null, null).ReturnsForAnyArgs(searchResult);

            await _exportExecutor.GetExportDataAsync(_exportRequest, jobProgress: null);

            await _searchService.DidNotReceive().InternalRequestForSearchAsync(
                Arg.Any<string>(),
                Arg.Is<List<Tuple<string, string>>>(x => x.Any(t => t.Item1 == KnownQueryParameterNames.ContinuationToken)),
                Arg.Any<SearchOptions>());
        }

        [Fact]
        public async Task GivenSearchServiceReturnsResources_WhenCallingGetExportDataAsync_ThenReturnsCorrectDeserialziedResources()
        {
            var observation1 = new Observation() { Id = "123" };
            var patient1 = new Patient() { Id = "abc" };

            ResourceWrapper[] resourceWrappers = new ResourceWrapper[]
            {
                new ResourceWrapper(observation1, _rawResourceFactory.Create(observation1), _resourceRequest, false, null, null, null),
                new ResourceWrapper(patient1, _rawResourceFactory.Create(patient1), _resourceRequest, false, null, null, null),
            };

            var searchResult = new SearchResult(resourceWrappers, continuationToken: null);
            _searchService.InternalRequestForSearchAsync(null, null, null).ReturnsForAnyArgs(searchResult);

            GetExportDataResult result = await _exportExecutor.GetExportDataAsync(_exportRequest, jobProgress: null);

            Assert.Equal(2, result.Resources.Count);
            Assert.True(ValidateIfResourcePresentInResult(observation1, result.Resources));
            Assert.True(ValidateIfResourcePresentInResult(patient1, result.Resources));
        }

        private static bool ValidateIfResourcePresentInResult(Resource expectedResource, List<Resource> resultResources)
        {
            foreach (Resource resource in resultResources)
            {
                if (expectedResource.Id.Equals(resource.Id) && expectedResource.ResourceType.Equals(resource.ResourceType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
