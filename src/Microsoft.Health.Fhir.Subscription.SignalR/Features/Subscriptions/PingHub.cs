// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.SignalR;

namespace Microsoft.Health.Fhir.Subscription.SignalR.Features.Subscriptions
{
    public class PingHub : Hub
    {
        public void Echo(string message)
        {
            Clients.Client(Context.ConnectionId).SendAsync("ping", message);
        }
    }
}
