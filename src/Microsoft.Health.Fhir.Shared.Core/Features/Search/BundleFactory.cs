// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Core;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Routing;
using Microsoft.Health.Fhir.Core.Features.Serialization;
using Microsoft.Health.Fhir.Core.Features.Serialization.SourceNodes.Models;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.ValueSets;

namespace Microsoft.Health.Fhir.Core.Features.Search
{
    public class BundleFactory : IBundleFactory
    {
        private readonly IUrlResolver _urlResolver;
        private readonly IFhirRequestContextAccessor _fhirRequestContextAccessor;
        private readonly ResourceDeserializer _deserializer;

        public BundleFactory(IUrlResolver urlResolver, IFhirRequestContextAccessor fhirRequestContextAccessor, ResourceDeserializer deserializer)
        {
            EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
            EnsureArg.IsNotNull(fhirRequestContextAccessor, nameof(fhirRequestContextAccessor));
            EnsureArg.IsNotNull(deserializer, nameof(deserializer));

            _urlResolver = urlResolver;
            _fhirRequestContextAccessor = fhirRequestContextAccessor;
            _deserializer = deserializer;
        }

        public ResourceElement CreateSearchBundle(SearchResult result)
        {
            return CreateBundle(result, "searchset", r =>
            {
                ResourceElement resource = _deserializer.Deserialize(r.Resource);

                return new BundleComponentJsonNode
                {
                    FullUrl = _urlResolver.ResolveResourceUrl(resource).ToString(),
                    Resource = resource.Resource,
                    Search = new BundleComponentSearchJsonNode
                    {
                        Mode = r.SearchEntryMode == SearchEntryMode.Match ? "match" : "include",
                    },
                };
            });
        }

        public ResourceElement CreateHistoryBundle(SearchResult result)
        {
            return CreateBundle(result, "history", r =>
            {
                var resource = _deserializer.Deserialize(r.Resource);
                var isPost = string.Equals("post", r.Resource.Request?.Method, StringComparison.OrdinalIgnoreCase);

                return new BundleComponentJsonNode
                {
                    FullUrl = _urlResolver.ResolveResourceUrl(resource, true).ToString(),
                    Resource = resource.Resource,
                    Request = new BundleComponentRequestJsonNode
                    {
                        Method = r.Resource.Request?.Method,
                        Url = !string.IsNullOrWhiteSpace(r.Resource.Request?.Method) ? $"{resource.InstanceType}/{(isPost ? null : resource.Id)}" : null,
                    },
                    Response = new BundleComponentResponseJsonNode
                    {
                        LastModified = r.Resource.LastModified.ToString("o"),
                        Etag = WeakETag.FromVersionId(r.Resource.Version).ToString(),
                    },
                };
            });
        }

        private ResourceElement CreateBundle(SearchResult result, string type, Func<SearchResultEntry, BundleComponentJsonNode> selectionFunction)
        {
            EnsureArg.IsNotNull(result, nameof(result));

            // Create the bundle from the result.
            var bundle = new BundleJsonNode();

            if (result != null)
            {
                IEnumerable<BundleComponentJsonNode> entries = result.Results.Select(selectionFunction);

                bundle.Entry = entries.ToArray();
                bundle.Link = new List<BundleLinkJsonNode>();

                if (result.ContinuationToken != null)
                {
                    bundle.Link.Add(new BundleLinkJsonNode
                    {
                        Relation = "next",
                        Url = _urlResolver.ResolveRouteUrl(
                        result.UnsupportedSearchParameters,
                        result.UnsupportedSortingParameters,
                        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(result.ContinuationToken)),
                        true).ToString(),
                    });
                }
            }

            // Add the self link to indicate which search parameters were used.
            bundle.Link.Add(new BundleLinkJsonNode
            {
                Relation = "self",
                Url = _urlResolver.ResolveRouteUrl(result.UnsupportedSearchParameters, result.UnsupportedSortingParameters).ToString(),
            });

            bundle.Id = _fhirRequestContextAccessor.FhirRequestContext.CorrelationId;
            bundle.Type = type;
            bundle.Total = result?.TotalCount;
            bundle.Meta = new MetaJsonNode
            {
                LastUpdated = Clock.UtcNow.ToString("o"),
            };

            return JsonSourceNodeFactory.Create(bundle).ToResourceElement(ModelInfoProvider.Instance);
        }
    }
}
