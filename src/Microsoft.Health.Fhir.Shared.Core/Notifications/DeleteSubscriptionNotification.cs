// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Model;

namespace Microsoft.Health.Fhir.Core.Notifications
{
    public class DeleteSubscriptionNotification
    {
        public DeleteSubscriptionNotification(Subscription subscription)
        {
            EnsureArg.IsNotNull(subscription, nameof(subscription));

            Subscription = subscription;
        }

        public Subscription Subscription { get; }
    }
}
