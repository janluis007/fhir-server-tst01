// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using EnsureThat;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Core.Notifications;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.Features.Subscriptions
{
    public class SubscriptionHandler : INotificationHandler<TopicHitNotification>
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

        public async Task Handle(TopicHitNotification topicHitNotification, CancellationToken cancellationToken)
        {
            if (!_subscriptionStore.IsInitialized)
            {
                _subscriptionStore.Start(_mediator);
            }

            foreach (var subscription in _subscriptionStore.GetForTopicId(topicHitNotification.TopicReference))
            {
                // Kick stuff out that needs to be filtered
                if (subscription.FilterBy.Count > 0)
                {
                }

                switch (subscription.Channel.Type.TextElement.ToString().ToUpperInvariant())
                {
                    case "REST HOOK":
                        if (_restSubscriptionNotifier != null)
                        {
                            _logger.LogInformation("Notifying via REST Hook call");
                            await _restSubscriptionNotifier.Notify(subscription);
                        }
                        else
                        {
                            _logger.LogWarning("No rest hook notifier found to alert on");
                        }

                        break;
                    case "WEBSOCKET":
                        if (_websocketSubscriptionNotifier != null)
                        {
                            _logger.LogInformation("Notifying via websocket call");
                            await _websocketSubscriptionNotifier.Ping(subscription);
                        }
                        else
                        {
                            _logger.LogWarning("No websocket notifier found to alert on");
                        }

                        break;
                }
            }
        }
    }
}
