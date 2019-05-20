// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Operations.Export.Models;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Messages.Export;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export
{
    public class ExportExecutor : IExportExecutor
    {
        private readonly ISearchService _searchService;
        private readonly ResourceDeserializer _resourceDeserializer;
        private readonly ExportJobConfiguration _exportJobConfiguration;

        public ExportExecutor(ISearchService searchService, ResourceDeserializer resourceDeserializer, IOptions<ExportJobConfiguration> exportJobConfiguration)
        {
            EnsureArg.IsNotNull(searchService, nameof(searchService));
            EnsureArg.IsNotNull(resourceDeserializer, nameof(resourceDeserializer));
            EnsureArg.IsNotNull(exportJobConfiguration?.Value, nameof(exportJobConfiguration));

            _searchService = searchService;
            _resourceDeserializer = resourceDeserializer;
            _exportJobConfiguration = exportJobConfiguration.Value;
        }

        public async Task<GetExportDataResult> GetExportDataAsync(CreateExportRequest exportRequest, ExportJobProgress jobProgress)
        {
            EnsureArg.IsNotNull(exportRequest, nameof(exportRequest));

            string continuationToken = null;
            if (jobProgress != null)
            {
                continuationToken = jobProgress.Query;
            }

            var queryParams = new List<Tuple<string, string>>();
            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                queryParams.Add(new Tuple<string, string>(KnownQueryParameterNames.ContinuationToken, continuationToken));
            }

            var searchOptions = new SearchOptions()
            {
                MaxItemCount = _exportJobConfiguration.MaxItemCountPerQuery,
            };

            SearchResult searchResult = await _searchService.InternalRequestForSearchAsync(exportRequest.ResourceType, queryParams, searchOptions);

            var getExportDataResult = new GetExportDataResult(searchResult.ContinuationToken);
            foreach (ResourceWrapper resourceWrapper in searchResult.Results)
            {
                Resource resource = _resourceDeserializer.DeserializeRaw(resourceWrapper.RawResource);
                getExportDataResult.Resources.Add(resource);
            }

            return getExportDataResult;
        }
    }
}
