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
    public class UpsertResourceNotificationHandler : INotificationHandler<UpsertResourceNotification>
    {
        private SubscriptionHandler _subscriptionHandler;

        public UpsertResourceNotificationHandler(SubscriptionHandler subscriptionHandler)
        {
            EnsureArg.IsNotNull(subscriptionHandler, nameof(subscriptionHandler));

            _subscriptionHandler = subscriptionHandler;
        }

        public Task Handle(UpsertResourceNotification notification, CancellationToken cancellationToken)
        {
            _subscriptionHandler.Handle(notification.Resource);

            return Task.CompletedTask;
        }
    }
}
