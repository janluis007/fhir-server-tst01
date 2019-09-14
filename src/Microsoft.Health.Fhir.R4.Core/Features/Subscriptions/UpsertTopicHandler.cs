// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Notifications;
using Microsoft.Health.Fhir.Shared.Core.Features.Subscriptions;

namespace Microsoft.Health.Fhir.Core.Features.Subscriptions
{
    public class UpsertTopicHandler : INotificationHandler<UpsertTopicNotification>
    {
        private readonly TopicStore _topicStore;

        public UpsertTopicHandler(TopicStore topicStore)
        {
            EnsureArg.IsNotNull(topicStore, nameof(topicStore));

            _topicStore = topicStore;
        }

        public Task Handle(UpsertTopicNotification notification, CancellationToken cancellationToken)
        {
            _topicStore.AddTopic(notification.Topic);

            return Task.CompletedTask;
        }
    }
}
