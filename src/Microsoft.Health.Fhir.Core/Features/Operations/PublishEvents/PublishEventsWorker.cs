// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Data;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Features.Operations.PublishEvents
{
    /// <summary>
    /// Publish Event Worker that reads change feed records from data source and writes events.
    /// </summary>
    public class PublishEventsWorker : IPublishEventsWorker
    {
        private readonly IChangeFeedSource<IResourceChangeData> _fhirResourcesChangeFeedStore;
        private readonly IChangeFeedSink<EventGridEvent> _eventSink;
        private readonly PublishEventsConfiguration _publishEventsConfiguration;
        private readonly ILogger _logger;

        /// <summary>
        /// Publish Events Job worker.
        /// </summary>
        /// <param name="fhirResourcesChangeFeedStore">Source Data Store</param>
        /// <param name="eventSink">Sink for the events to be written to.</param>
        /// <param name="publishEventsJobConfiguration">Configuration</param>
        /// <param name="logger">Logger</param>
        public PublishEventsWorker(
            IChangeFeedSource<IResourceChangeData> fhirResourcesChangeFeedStore,
            IChangeFeedSink<EventGridEvent> eventSink,
            IOptions<PublishEventsConfiguration> publishEventsJobConfiguration,
            ILogger<PublishEventsWorker> logger)
        {
            _fhirResourcesChangeFeedStore = fhirResourcesChangeFeedStore;
            _eventSink = eventSink;
            _publishEventsConfiguration = publishEventsJobConfiguration.Value;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            long lastProcessedId = 0;

            var resourceChangeTypeMap = new Dictionary<short, string>
            {
                { 0, "Microsoft.HealthcareApis.FhirResourceCreated" },
                { 1, "Microsoft.HealthcareApis.FhirResourceUpdated" },
                { 2, "Microsoft.HealthcareApis.FhirResourceDeleted" },
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Set startId as last successfully processed record Id plus 1.
                    long startId = lastProcessedId + 1;

                    IReadOnlyCollection<IResourceChangeData> records =
                        await _fhirResourcesChangeFeedStore.GetRecordsAsync(startId, 25, cancellationToken);

                    if (records.Count > 0)
                    {
                        // Publish events.
                        var events = records.Select(r => new EventGridEvent(
                            $"{_publishEventsConfiguration.FhirAccount}/{r.ResourceTypeName}/{r.ResourceId}",
                            eventType: resourceChangeTypeMap[r.ResourceChangeTypeId],
                            dataVersion: r.ResourceVersion.ToString(CultureInfo.InvariantCulture),
                            new BinaryData(new
                                {
                                    ResourceType = r.ResourceTypeName,
                                    ResourceFhirAccount = _publishEventsConfiguration.FhirAccount,
                                    ResourceFhirId = r.ResourceId,
                                    ResourceVersionId = r.ResourceVersion,
                                }))
                        {
                            Topic = _publishEventsConfiguration.EventGridTopic,
                            Id = r.Id.ToString(CultureInfo.InvariantCulture),
                            EventTime = r.Timestamp,
                        }).ToList();

                        await _eventSink.WriteAsync(events);

                        int count = records.Count;
                        _logger.LogInformation($@"Published {count} records by reading change feed ");

                        // Update watermark.
                        startId += records.Count;
                        lastProcessedId = startId;
                    }

                    await Task.Delay(_publishEventsConfiguration.JobPollingFrequency, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // End the execution of the task
                }
                catch (Exception ex)
                {
                    _logger.LogError(exception: ex, message: "Error occurred on PublishEventsWorker.ExecuteAsync");
                }
            }
        }
    }
}
