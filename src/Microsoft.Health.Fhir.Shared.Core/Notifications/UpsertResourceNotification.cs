// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Model;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Persistence;

namespace Microsoft.Health.Fhir.Core.Notifications
{
    public class UpsertResourceNotification : INotification
    {
        public UpsertResourceNotification(Resource resource, ResourceWrapper resourceWrapper, string interaction)
        {
            EnsureArg.IsNotNull(resource, nameof(resource));
            EnsureArg.IsNotNull(resourceWrapper, nameof(resourceWrapper));
            EnsureArg.IsNotNullOrWhiteSpace(interaction, nameof(interaction));

            Resource = resource;
            ResourceWrapper = resourceWrapper;
            Interaction = interaction;
        }

        public Resource Resource { get; }

        public ResourceWrapper ResourceWrapper { get; }

        public string Interaction { get; }
    }
}
