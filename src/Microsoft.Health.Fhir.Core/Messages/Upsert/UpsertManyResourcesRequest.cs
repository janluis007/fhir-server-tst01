// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Conformance;
using Microsoft.Health.Fhir.Core.Features.Persistence;

namespace Microsoft.Health.Fhir.Core.Messages.Upsert
{
    public class UpsertManyResourcesRequest : IRequest<UpsertManyResourcesResponse>, IRequest, IRequireCapability
    {
        public UpsertManyResourcesRequest(List<ResourceWrapper> resources)
        {
            EnsureArg.IsNotNull(resources, nameof(resources));

            Resources = resources;
        }

        public List<ResourceWrapper> Resources { get; }

        public IEnumerable<CapabilityQuery> RequiredCapabilities()
        {
            yield break;
        }
    }
}
