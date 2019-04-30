// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Health.Fhir.Core.Features.Subscriptions;

namespace Microsoft.Health.Fhir.Subscription.SignalR.Features.Subscriptions
{
    public class SignalRSubscriptionNotifier : IWebsocketSubscriptionNotifier
    {
        public Task Ping(Hl7.Fhir.Model.Subscription subscription)
        {
            throw new System.NotImplementedException();
        }
    }
}
