// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Fhir.Core.Features.Subscriptions;

namespace Microsoft.Health.Fhir.Subscription.Rest.Features.Subscriptions
{
    public class RestSubscriptionNotifier : IRestSubscriptionNotifier
    {
        private IHttpClientFactory _httpClientFactory;

        public RestSubscriptionNotifier(IHttpClientFactory httpClientFactory)
        {
            EnsureArg.IsNotNull(httpClientFactory, nameof(httpClientFactory));

            _httpClientFactory = httpClientFactory;
        }

        public async Task Notify(Hl7.Fhir.Model.Subscription subscription)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                await httpClient.PostAsync(new Uri(subscription.Channel.Endpoint), new MultipartFormDataContent());
            }
        }
    }
}
