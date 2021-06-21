// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Fhir.Core.Configs
{
    /// <summary>
    /// Publish Events Configuration
    /// </summary>
    public class PublishEventsConfiguration
    {
        /// <summary>
        /// Determines whether publish events feature is enabled or not.
        /// </summary>
        public bool Enabled { get; set; }

        public TimeSpan JobPollingFrequency { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// EventGridTopicEndPoint.
        /// </summary>
        public string EventGridTopicEndPoint { get; set; }

        /// <summary>
        /// Access key for event grid if used.
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Fhir Account
        /// </summary>
        public string FhirAccount { get; set; }

        /// <summary>
        /// Event Grid Topic
        /// </summary>
        public string EventGridTopic { get; set; }
    }
}
