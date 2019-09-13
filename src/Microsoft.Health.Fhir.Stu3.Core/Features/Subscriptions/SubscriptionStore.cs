// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using MediatR;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Messages.Search;

namespace Microsoft.Health.Fhir.Core.Features.Subscriptions
{
    public class SubscriptionStore
    {
        private readonly Dictionary<string, Subscription> _subscriptions;

        public SubscriptionStore()
        {
            _subscriptions = new Dictionary<string, Subscription>();
        }

        public bool IsInitialized { get; private set; }

        public IReadOnlyList<Subscription> GetForResourceType()
        {
            return _subscriptions.Select(x => x.Value).ToList();
        }

        public void AddSubscription(Subscription subscription)
        {
            string subscriptionId = subscription.Id;

            if (_subscriptions.ContainsKey(subscriptionId))
            {
                _subscriptions[subscriptionId] = subscription;
            }
            else
            {
                _subscriptions.Add(subscriptionId, subscription);
            }
        }

        public void Start(IMediator mediator)
        {
            IsInitialized = true;
            var result = mediator.Send(new SearchResourceRequest("Subscription", new List<Tuple<string, string>>())).GetAwaiter().GetResult();

            var subscriptions = result.Bundle.ToPoco<Bundle>();

            do
            {
                foreach (Bundle.EntryComponent bundleEntry in subscriptions.Entry)
                {
                    AddSubscription((Subscription)bundleEntry.Resource);
                }

                if (subscriptions.NextLink != null)
                {
                    // TODO: BKP - Figure out how to do the continuation to get them all
                    subscriptions.NextLink = null;
                }
            }
            while (subscriptions.NextLink != null);
        }
    }
}
