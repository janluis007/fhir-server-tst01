// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Sqlite.Features.Storage;

namespace Microsoft.Health.Fhir.Sqlite.Features.Search
{
    public class SqliteSearchService : SearchService
    {
        private readonly SqliteFhirDataStore _fhirDataStore;

        public SqliteSearchService(
            ISearchOptionsFactory searchOptionsFactory,
            SqliteFhirDataStore fhirDataStore)
            : base(searchOptionsFactory, fhirDataStore)
        {
            EnsureArg.IsNotNull(fhirDataStore, nameof(fhirDataStore));

            _fhirDataStore = fhirDataStore;
        }

        protected override Task<SearchResult> SearchInternalAsync(
            SearchOptions searchOptions,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task<SearchResult> SearchHistoryInternalAsync(
            SearchOptions searchOptions,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
