// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Hl7.Fhir.Model;

namespace Microsoft.Health.Fhir.Core.Features.Resources
{
    public class TopicResourceModifier : IResourceModifier
    {
        public Type TargetType => typeof(Topic);

        public void Modify(Resource resource)
        {
            dynamic changedObj = Convert.ChangeType(resource, TargetType);

            changedObj.Status = PublicationStatus.Active;
        }
    }
}
