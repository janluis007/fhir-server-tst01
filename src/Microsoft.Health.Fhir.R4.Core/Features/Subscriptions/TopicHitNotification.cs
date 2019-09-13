// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Model;
using MediatR;

namespace Microsoft.Health.Fhir.Core.Notifications
{
    public class TopicHitNotification : INotification
    {
        public TopicHitNotification(string topicReference, Resource resource)
        {
            EnsureArg.IsNotNullOrWhiteSpace(topicReference, nameof(topicReference));
            EnsureArg.IsNotNull(resource, nameof(resource));

            TopicReference = topicReference;
            Resource = resource;
        }

        public string TopicReference { get; set; }

        public Resource Resource { get; }
    }
}
