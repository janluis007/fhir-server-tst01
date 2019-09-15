// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Core.Features.Subscriptions;

namespace Microsoft.Health.Fhir.Core.Features.Resources
{
    public class SubscriptionResourceModifier : IResourceModifier
    {
        public SubscriptionResourceModifier(IRestSubscriptionNotifier restSubscriptionNotifier)
        {
            EnsureArg.IsNotNull(restSubscriptionNotifier, nameof(restSubscriptionNotifier));

            RestSubscriptionNotifier = restSubscriptionNotifier;
        }

        public IRestSubscriptionNotifier RestSubscriptionNotifier { get; set; }

        public Type TargetType => typeof(Subscription);

        public void Modify(Resource resource)
        {
            dynamic changedObj = Convert.ChangeType(resource, TargetType);
            if (changedObj.Status == Subscription.SubscriptionStatus.Requested)
            {
                changedObj.Status = Subscription.SubscriptionStatus.Active;
            }
        }
    }
}
