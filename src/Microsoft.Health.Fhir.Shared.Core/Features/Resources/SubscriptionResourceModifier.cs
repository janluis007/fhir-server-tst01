// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Hl7.Fhir.Model;

namespace Microsoft.Health.Fhir.Core.Features.Resources
{
    public class SubscriptionResourceModifier : IResourceModifier
    {
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
