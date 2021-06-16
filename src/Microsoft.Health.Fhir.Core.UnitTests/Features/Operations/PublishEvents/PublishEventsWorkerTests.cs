// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Data;
using Microsoft.Health.Abstractions.Features.Events;
using Microsoft.Health.Core.Features.Events;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Operations.PublishEvents;
using Microsoft.Health.Fhir.Core.Models;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Fhir.Core.UnitTests.Features.Operations.PublishEvents
{
    public class PublishEventsWorkerTests
    {
        private readonly IPublishEventsWorker _publishEventsWorker;
        private readonly IResourceChangeData _dbResourceChangeTestData;
        private List<EventGridEvent> _publishedEventGridEvents;
        private readonly IOptions<PublishEventsConfiguration> _publishEventsConfigOptions;

        public PublishEventsWorkerTests()
        {
            _dbResourceChangeTestData = new ResourceChangeData
            {
                Id = 1,
                ResourceChangeTypeId = 0,
                ResourceId = Guid.NewGuid().ToString(),
                ResourceTypeId = 103,
                ResourceTypeName = "Patient",
                ResourceVersion = 1,
                Timestamp = DateTime.UtcNow,
            };

            _publishedEventGridEvents = new List<EventGridEvent>();

            var resourceChangeRecords = new List<IResourceChangeData> { _dbResourceChangeTestData };

            IChangeFeedSource<IResourceChangeData> changeFeedSource = NSubstitute.Substitute.For<IChangeFeedSource<IResourceChangeData>>();
            changeFeedSource
                .GetRecordsAsync(Arg.Any<long>(), Arg.Any<int>(), default)
                .ReturnsForAnyArgs(new ReadOnlyCollection<IResourceChangeData>(resourceChangeRecords));

            IEventGridPublisher eventGridPublisher = Substitute.For<IEventGridPublisher>();
            eventGridPublisher
                .WhenForAnyArgs(x => x.SendEventsAsync(Arg.Any<IEnumerable<EventGridEvent>>()))
                .Do(events =>
                {
                    _publishedEventGridEvents.AddRange(events.ArgAt<IEnumerable<EventGridEvent>>(0));
                });

            IChangeFeedSink<EventGridEvent> eventGridSink = new EventGridSink(eventGridPublisher);

            _publishEventsConfigOptions = NSubstitute.Substitute.For<IOptions<PublishEventsConfiguration>>();
            _publishEventsConfigOptions.Value.Returns(new PublishEventsConfiguration()
            {
                JobPollingFrequency = TimeSpan.FromSeconds(1),
                FhirAccount = "testFhir.contoso.healthcare",
            });

            ILogger<PublishEventsWorker> logger = NSubstitute.Substitute.For<ILogger<PublishEventsWorker>>();

            _publishEventsWorker = new PublishEventsWorker(
                changeFeedSource,
                eventGridSink,
                _publishEventsConfigOptions,
                logger);
        }

        [Fact]
        public void PublishEventsWorker_ShouldPublishEvents()
        {
            var resourceChangeTypeMap = new Dictionary<short, string>
            {
                { 0, "Microsoft.HealthcareApis.FhirResourceCreated" },
                { 1, "Microsoft.HealthcareApis.FhirResourceUpdated" },
                { 2, "Microsoft.HealthcareApis.FhirResourceDeleted" },
            };

            var tokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = tokenSource.Token;

            _publishEventsWorker.ExecuteAsync(cancellationToken);

            var expectedBinaryData = new BinaryData(new
            {
                ResourceType = _dbResourceChangeTestData.ResourceTypeName,
                ResourceFhirAccount = _publishEventsConfigOptions.Value.FhirAccount,
                ResourceFhirId = _dbResourceChangeTestData.ResourceId,
                ResourceVersionId = _dbResourceChangeTestData.ResourceVersion,
            });

            // Assert the Database Records and the Generated Event match.
            Assert.Equal(_dbResourceChangeTestData.Id.ToString(), _publishedEventGridEvents[0].Id);
            Assert.Equal(_dbResourceChangeTestData.ResourceVersion.ToString(), _publishedEventGridEvents[0].DataVersion);
            Assert.Equal(_dbResourceChangeTestData.Timestamp, _publishedEventGridEvents[0].EventTime.DateTime);
            Assert.Equal(resourceChangeTypeMap[_dbResourceChangeTestData.ResourceChangeTypeId], _publishedEventGridEvents[0].EventType);
            Assert.Equal(expectedBinaryData.ToString(), _publishedEventGridEvents[0].Data.ToString());

            tokenSource.CancelAfter(TimeSpan.FromSeconds(1));
        }
    }
}
