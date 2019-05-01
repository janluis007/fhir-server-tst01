// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Fhir.Api.Features.ActionResults;
using Microsoft.Health.Fhir.Core.Features.Routing;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Fhir.Api.Features.Headers
{
    public static class BaseActionResultExtensions
    {
        // Generates the url to be included in the response based on the operation and sets the content location header.
        public static TActionResult SetContentLocationHeader<TActionResult, TResult>(this TActionResult result, IUrlResolver urlResolver, string routeName, string id)
            where TActionResult : BaseActionResult<TResult>
        {
            EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
            EnsureArg.IsNotNullOrWhiteSpace(routeName, nameof(routeName));
            EnsureArg.IsNotNullOrWhiteSpace(id, nameof(id));

            var url = urlResolver.ResolveOperationResultUrl(routeName, id);

            result.Headers.Add(HeaderNames.ContentLocation, url.ToString());
            return result;
        }

        public static TActionResult SetContentTypeHeader<TActionResult, TResult>(this TActionResult result, string contentTypeValue)
            where TActionResult : BaseActionResult<TResult>
        {
            EnsureArg.IsNotNullOrWhiteSpace(contentTypeValue);

            result.Headers.Add(HeaderNames.ContentType, contentTypeValue);
            return result;
        }
    }
}
