// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Hl7.Fhir.Model;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Fhir.Core.Features.Subscriptions
{
    public class SubscriptionHandler
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SubscriptionHandler> _logger;
        private readonly SubscriptionStore _subscriptionStore;
        private readonly IRestSubscriptionNotifier _restSubscriptionNotifier;
        private readonly IWebsocketSubscriptionNotifier _websocketSubscriptionNotifier;

        public SubscriptionHandler(IMediator mediator, SubscriptionStore subscriptionStore, IRestSubscriptionNotifier restSubscriptionNotifier, IWebsocketSubscriptionNotifier websocketSubscriptionNotifier, ILogger<SubscriptionHandler> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(subscriptionStore, nameof(subscriptionStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _subscriptionStore = subscriptionStore;
            _restSubscriptionNotifier = restSubscriptionNotifier;
            _websocketSubscriptionNotifier = websocketSubscriptionNotifier;
            _logger = logger;
        }

        public void Handle(Resource notificationResource)
        {
            var test = notificationResource;
            if (!_subscriptionStore.IsInitialized)
            {
                _subscriptionStore.Start(_mediator);
            }

            foreach (var subscription in _subscriptionStore.GetForResourceType())
            {
                // TODO: BKP Implemement topic matching
                ////string rootCharacter = string.Empty;
                ////if (subscription.Criteria.StartsWith("/", StringComparison.InvariantCulture))
                ////{
                ////    rootCharacter = "/";
                ////}

                ////if (subscription.Criteria.StartsWith($"{rootCharacter}{notificationResource.ResourceType}", StringComparison.InvariantCulture))
                ////{
                ////    switch (subscription.Channel.Type)
                ////    {
                ////        case Subscription.SubscriptionChannelType.RestHook:
                ////            if (_restSubscriptionNotifier != null)
                ////            {
                ////                _logger.LogInformation("Notifying via REST Hook call");
                ////                _restSubscriptionNotifier.Notify(subscription);
                ////            }
                ////            else
                ////            {
                ////                _logger.LogWarning("No rest hook notifier found to alert on");
                ////            }

                ////            break;
                ////        case Subscription.SubscriptionChannelType.Websocket:
                ////            if (_websocketSubscriptionNotifier != null)
                ////            {
                ////                _logger.LogInformation("Notifying via websocket call");
                ////                _websocketSubscriptionNotifier.Ping(subscription);
                ////            }
                ////            else
                ////            {
                ////                _logger.LogWarning("No websocket notifier found to alert on");
                ////            }

                ////            break;
                ////    }
                ////}
            }
        }
    }
}
