// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Model;
using MediatR;

namespace Microsoft.Health.Fhir.Core.Notifications
{
    public class UpsertResourceNotification : INotification
    {
        public UpsertResourceNotification(Resource resource, string interaction)
        {
            EnsureArg.IsNotNull(resource, nameof(resource));
            EnsureArg.IsNotNullOrWhiteSpace(interaction, nameof(interaction));

            Resource = resource;
            Interaction = interaction;
        }

        public Resource Resource { get; }

        public string Interaction { get; }
    }
}
