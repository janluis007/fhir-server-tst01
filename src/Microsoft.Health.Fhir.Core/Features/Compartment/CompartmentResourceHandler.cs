// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using MediatR;
using Microsoft.Health.Fhir.Core.Messages.Search;

namespace Microsoft.Health.Fhir.Core.Features.Search
{
    /// <summary>
    /// Handler for searching resource based on compartment.
    /// </summary>
    public class CompartmentResourceHandler : IRequestHandler<CompartmentResourceRequest, CompartmentResourceResponse>
    {
        private readonly ISearchService _searchService;
        private readonly IBundleFactory _bundleFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompartmentResourceHandler"/> class.
        /// </summary>
        /// <param name="searchService">The search service to execute the search operation.</param>
        /// <param name="bundleFactory">The bundle factory.</param>
        public CompartmentResourceHandler(ISearchService searchService, IBundleFactory bundleFactory)
        {
            EnsureArg.IsNotNull(searchService, nameof(searchService));
            EnsureArg.IsNotNull(bundleFactory, nameof(bundleFactory));

            _searchService = searchService;
            _bundleFactory = bundleFactory;
        }

        /// <inheritdoc />
        public async Task<CompartmentResourceResponse> Handle(CompartmentResourceRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            SearchResult searchResult = await _searchService.SearchCompartmentAsync(message.CompartmentType, message.CompartmentId, message.ResourceType, message.Queries, cancellationToken);

            Bundle bundle = _bundleFactory.CreateSearchBundle(searchResult);

            return new CompartmentResourceResponse(bundle);
        }
    }
}
