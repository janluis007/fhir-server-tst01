// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Model;

namespace Microsoft.Health.Fhir.Core.Notifications
{
    public class DeleteTopicNotification
    {
        public DeleteTopicNotification(Topic topic)
        {
            EnsureArg.IsNotNull(topic, nameof(topic));

            Topic = topic;
        }

        public Topic Topic { get; }
    }
}
