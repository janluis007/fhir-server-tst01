// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Notifications;

namespace Microsoft.Health.Fhir.Core.Features.Subscriptions
{
    public class UpsertSubscriptionHandler : INotificationHandler<UpsertSubscriptionNotification>
    {
        private readonly SubscriptionStore _subscriptionStore;

        public UpsertSubscriptionHandler(SubscriptionStore subscriptionStore)
        {
            EnsureArg.IsNotNull(subscriptionStore, nameof(subscriptionStore));

            _subscriptionStore = subscriptionStore;
        }

        public Task Handle(UpsertSubscriptionNotification notification, CancellationToken cancellationToken)
        {
            _subscriptionStore.AddSubscription(notification.Subscription);

            return Task.CompletedTask;
        }
    }
}
