// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Fhir.Core.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import.Models
{
    /// <summary>
    /// Class to hold metadata for an individual import request.
    /// </summary>
    public class ImportJobRecord
    {
        public ImportJobRecord(Uri importRequestUri, ImportRequest request)
        {
            EnsureArg.IsNotNull(importRequestUri, nameof(importRequestUri));
            EnsureArg.IsNotNull(request, nameof(request));

            RequestUri = importRequestUri;
            Request = request;

            // Default values
            SchemaVersion = 1;
            Status = OperationStatus.Queued;
            Id = Guid.NewGuid().ToString();
            QueuedTime = DateTimeOffset.UtcNow;
        }

        [JsonConstructor]
        protected ImportJobRecord()
        {
        }

        [JsonProperty("requestUri")]
        public Uri RequestUri { get; private set; }

        [JsonProperty("request")]
        public ImportRequest Request { get; private set; }

        [JsonProperty(JobRecordProperties.Id)]
        public string Id { get; private set; }

        [JsonProperty(JobRecordProperties.Hash)]
        public string Hash { get; private set; }

        [JsonProperty(JobRecordProperties.QueuedTime)]
        public DateTimeOffset QueuedTime { get; private set; }

        [JsonProperty(JobRecordProperties.SchemaVersion)]
        public int SchemaVersion { get; private set; }

        [JsonProperty(JobRecordProperties.Error)]
        public IProducerConsumerCollection<OperationOutcomeIssue> Errors { get; private set; } = new ConcurrentBag<OperationOutcomeIssue>();

        [JsonProperty(JobRecordProperties.Status)]
        public OperationStatus Status { get; set; }

        [JsonProperty(JobRecordProperties.StartTime)]
        public DateTimeOffset? StartTime { get; set; }

        [JsonProperty(JobRecordProperties.EndTime)]
        public DateTimeOffset? EndTime { get; set; }

        [JsonProperty(JobRecordProperties.CanceledTime)]
        public DateTimeOffset? CanceledTime { get; set; }

        [JsonProperty(JobRecordProperties.Progress)]
        public IList<ImportJobProgress> Progress { get; } = new List<ImportJobProgress>();
    }
}
