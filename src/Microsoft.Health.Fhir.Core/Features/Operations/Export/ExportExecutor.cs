// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Core.Features.Operations.Export.Models;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Messages.Export;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export
{
    public class ExportExecutor : IExportExecutor
    {
        private ISearchService _searchService;
        private ResourceDeserializer _resourceDeserializer;

        public ExportExecutor(ISearchService searchService, ResourceDeserializer resourceDeserializer)
        {
            EnsureArg.IsNotNull(searchService, nameof(searchService));
            EnsureArg.IsNotNull(resourceDeserializer, nameof(resourceDeserializer));

            _searchService = searchService;
            _resourceDeserializer = resourceDeserializer;
        }

        public async Task<GetExportDataResult> GetExportDataAsync(CreateExportRequest exportRequest, ExportJobProgress jobProgress, int maxCountPerQuery = 100)
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

            SearchResult searchResult = await _searchService.InternalRequestForSearchAsync(resourceType: null, queryParams);

            var getExportDataResult = new GetExportDataResult(searchResult.ContinuationToken);
            foreach (var resourceWrapper in searchResult.Results)
            {
                Resource r = _resourceDeserializer.DeserializeRaw(resourceWrapper.RawResource);
                getExportDataResult.Resources.Add(r);
            }

            return getExportDataResult;
        }
    }
}
