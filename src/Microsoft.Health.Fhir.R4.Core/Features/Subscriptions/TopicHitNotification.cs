// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Hl7.Fhir.Model;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Search;

namespace Microsoft.Health.Fhir.Core.Notifications
{
    public class TopicHitNotification : INotification
    {
        public TopicHitNotification(Topic topic, Resource resource, List<SearchIndexEntry> filterCriteria)
        {
            EnsureArg.IsNotNull(topic, nameof(topic));
            EnsureArg.IsNotNull(resource, nameof(resource));
            EnsureArg.IsNotNull(filterCriteria, nameof(filterCriteria));

            Topic = topic;
            Resource = resource;
            FilterCriteria = filterCriteria;
        }

        public List<SearchIndexEntry> FilterCriteria { get; }

        public string TopicReference => $"Topic/{Topic.Id}";

        public Topic Topic { get; }

        public Resource Resource { get; }
    }
}
