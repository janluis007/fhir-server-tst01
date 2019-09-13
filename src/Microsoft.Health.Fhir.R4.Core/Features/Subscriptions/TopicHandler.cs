// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using EnsureThat;
using Hl7.Fhir.Model;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Core.Notifications;
using Microsoft.Health.Fhir.Shared.Core.Features.Subscriptions;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.Features.Subscriptions
{
    public class TopicHandler : INotificationHandler<UpsertResourceNotification>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TopicHandler> _logger;
        private readonly TopicStore _topicStore;
        private readonly IRestSubscriptionNotifier _restSubscriptionNotifier;
        private readonly IWebsocketSubscriptionNotifier _websocketSubscriptionNotifier;

        public TopicHandler(IMediator mediator, TopicStore topicStore, IRestSubscriptionNotifier restSubscriptionNotifier, IWebsocketSubscriptionNotifier websocketSubscriptionNotifier, ILogger<TopicHandler> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(topicStore, nameof(topicStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _topicStore = topicStore;
            _restSubscriptionNotifier = restSubscriptionNotifier;
            _websocketSubscriptionNotifier = websocketSubscriptionNotifier;
            _logger = logger;
        }

        public async Task Handle(UpsertResourceNotification upsertResourceNotification, CancellationToken cancellationToken)
        {
            var notificationResource = upsertResourceNotification.Resource;
            if (!_topicStore.IsInitialized)
            {
                _topicStore.Start(_mediator);
            }

            foreach (var topic in _topicStore.GetForResourceType(notificationResource.TypeName))
            {
                if (topic.Status != PublicationStatus.Active)
                {
                    continue;
                }

                // check method criteria
                if (topic.ResourceTrigger.MethodCriteria != null)
                {
                }

                // Check the current query criteria
                if (!string.IsNullOrWhiteSpace(topic.ResourceTrigger.QueryCriteria.Current))
                {
                }

                // Check the previous query criteria
                if (!string.IsNullOrWhiteSpace(topic.ResourceTrigger.QueryCriteria.Previous))
                {
                }

                if (!string.IsNullOrWhiteSpace(topic.ResourceTrigger.FhirPathCriteria))
                {
                }

                await _mediator.Publish(new TopicHitNotification($"Topic/{topic.Id}", notificationResource), cancellationToken);
            }
        }
    }
}
