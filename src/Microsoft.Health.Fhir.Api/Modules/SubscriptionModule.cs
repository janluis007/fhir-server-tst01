// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Features.Subscriptions;

namespace Microsoft.Health.Fhir.Api.Modules
{
    public class SubscriptionModule : IStartupModule
    {
        public void Load(IServiceCollection services)
        {
            services.Add<SubscriptionStore>()
                .Singleton()
                .AsSelf();

            services.Add<SubscriptionHandler>()
                .Scoped()
                .AsSelf();
        }
    }
}
