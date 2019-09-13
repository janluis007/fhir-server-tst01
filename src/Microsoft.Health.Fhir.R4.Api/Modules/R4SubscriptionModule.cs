// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Features.Resources;
using Microsoft.Health.Fhir.Core.Features.Subscriptions;
using Microsoft.Health.Fhir.Shared.Core.Features.Subscriptions;

namespace Microsoft.Health.Fhir.Api.Modules
{
    public class R4SubscriptionModule : IStartupModule
    {
        public void Load(IServiceCollection services)
        {
            services.Add<TopicStore>()
                .Singleton()
                .AsSelf();

            services.Add<TopicHandler>()
                .Scoped()
                .AsSelf();

            services.Add<TopicResourceModifier>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();
        }
    }
}
