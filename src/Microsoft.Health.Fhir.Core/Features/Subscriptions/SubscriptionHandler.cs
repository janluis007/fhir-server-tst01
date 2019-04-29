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

        public SubscriptionHandler(IMediator mediator, SubscriptionStore subscriptionStore, ILogger<SubscriptionHandler> logger)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(subscriptionStore, nameof(subscriptionStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _mediator = mediator;
            _subscriptionStore = subscriptionStore;
            _logger = logger;
        }

        public void Handle(Resource notificationResource)
        {
            if (!_subscriptionStore.IsInitialized)
            {
                _subscriptionStore.Start(_mediator);
            }

            foreach (var subscription in _subscriptionStore.GetForResourceType())
            {
                string rootCharacter = string.Empty;
                if (subscription.Criteria.StartsWith("/", StringComparison.InvariantCulture))
                {
                    rootCharacter = "/";
                }

                if (subscription.Criteria.StartsWith($"{rootCharacter}{notificationResource.ResourceType}", StringComparison.InvariantCulture))
                {
                    _logger.LogInformation("WINNER! WINNER! CHICKEN DINNER!");
                }
            }
        }
    }
}
