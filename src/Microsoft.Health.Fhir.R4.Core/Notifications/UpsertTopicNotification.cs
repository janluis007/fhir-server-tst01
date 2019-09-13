// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Model;
using MediatR;

namespace Microsoft.Health.Fhir.Core.Notifications
{
    public class UpsertTopicNotification : INotification
    {
        public UpsertTopicNotification(Topic topic)
        {
            EnsureArg.IsNotNull(topic, nameof(topic));

            Topic = topic;
        }

        public Topic Topic { get; }
    }
}
