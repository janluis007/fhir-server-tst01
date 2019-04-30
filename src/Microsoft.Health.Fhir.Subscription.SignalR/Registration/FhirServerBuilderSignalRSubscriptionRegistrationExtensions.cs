// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Subscription.SignalR.Features.Subscriptions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FhirServerBuilderSignalRSubscriptionRegistrationExtensions
    {
        public static IServiceCollection AddSignalRSubscription(this IServiceCollection serviceCollection)
        {
            EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));

            serviceCollection.Add<SignalRSubscriptionNotifier>()
                .Singleton()
                .AsImplementedInterfaces();

            return serviceCollection;
        }
    }
}
