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
        private readonly Dictionary<string, Subscription> _subscriptionsById;
        private readonly Dictionary<string, Dictionary<string, Subscription>> _subscriptionsByTopicId;

        public SubscriptionStore()
        {
            _subscriptionsById = new Dictionary<string, Subscription>();
            _subscriptionsByTopicId = new Dictionary<string, Dictionary<string, Subscription>>();
        }

        public bool IsInitialized { get; private set; }

        public IReadOnlyList<Subscription> GetForTopicId(string topicId)
        {
            if (!_subscriptionsByTopicId.ContainsKey(topicId))
            {
                return Enumerable.Empty<Subscription>().ToList();
            }

            return _subscriptionsByTopicId[topicId].Select(x => x.Value).ToList();
        }

        public void AddSubscription(Subscription subscription)
        {
            string subscriptionId = subscription.Id;

            if (_subscriptionsById.ContainsKey(subscriptionId))
            {
                _subscriptionsById[subscriptionId] = subscription;
            }
            else
            {
                _subscriptionsById.Add(subscriptionId, subscription);
            }

            if (_subscriptionsByTopicId.ContainsKey(subscription.Topic.Reference))
            {
                if (_subscriptionsByTopicId[subscription.Topic.Reference].ContainsKey(subscriptionId))
                {
                    _subscriptionsByTopicId[subscription.Topic.Reference][subscriptionId] = subscription;
                }
                else
                {
                    _subscriptionsByTopicId[subscription.Topic.Reference].Add(subscriptionId, subscription);
                }
            }
            else
            {
                _subscriptionsByTopicId.Add(subscription.Topic.Reference, new Dictionary<string, Subscription> { { subscriptionId, subscription } });
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
