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
    public static class ImportResultExtensions
    {
        // Generates the url to be included in the response based on the operation and sets the content location header.
        public static ImportResult SetContentLocationHeader(this ImportResult importResult, IUrlResolver urlResolver, string operationName, string id)
        {
            EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
            EnsureArg.IsNotNullOrWhiteSpace(operationName, nameof(operationName));
            EnsureArg.IsNotNullOrWhiteSpace(id, nameof(id));

            var url = urlResolver.ResolveOperationResultUrl(operationName, id);

            importResult.Headers.Add(HeaderNames.ContentLocation, url.ToString());
            return importResult;
        }

        public static ImportResult SetContentTypeHeader(this ImportResult importResult, string contentTypeValue)
        {
            EnsureArg.IsNotNullOrWhiteSpace(contentTypeValue);

            importResult.Headers.Add(HeaderNames.ContentType, contentTypeValue);
            return importResult;
        }
    }
}
