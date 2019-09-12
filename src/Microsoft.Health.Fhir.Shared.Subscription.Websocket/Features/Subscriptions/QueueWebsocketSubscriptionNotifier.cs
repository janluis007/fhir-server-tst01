// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Conformance;
using Microsoft.Health.Fhir.Core.Features.Subscriptions;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Subscription.Websocket.Features.Subscriptions
{
    public class QueueWebsocketSubscriptionNotifier : IWebsocketSubscriptionNotifier, IProvideCapability
    {
        private readonly CloudQueue _queue;
        private readonly IConfiguredConformanceProvider _configuredConformanceProvider;
        private readonly SubscriptionConfiguration _subscriptionConfiguration;

        public QueueWebsocketSubscriptionNotifier(IOptions<SubscriptionConfiguration> subscriptionConfigurationOptions, IConfiguredConformanceProvider configuredConformanceProvider, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(subscriptionConfigurationOptions?.Value, nameof(subscriptionConfigurationOptions));
            EnsureArg.IsNotNull(configuredConformanceProvider, nameof(configuredConformanceProvider));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            var storageConnectionString = configuration["Azure:Storage:ConnectionString"];

            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            _queue = queueClient.GetQueueReference("websocket");

            // Create the queue if it doesn't already exist
            _queue.CreateIfNotExists();

            _configuredConformanceProvider = configuredConformanceProvider;
            _subscriptionConfiguration = subscriptionConfigurationOptions.Value;
        }

        public async Task Ping(Hl7.Fhir.Model.Subscription subscription)
        {
            // Create a message and add it to the queue.
            var message = new CloudQueueMessage(subscription.Id);
            await _queue.AddMessageAsync(message);
        }

        public void Build(IListedCapabilityStatement statement)
        {
            var listedCapabilityStatement = statement as ListedCapabilityStatement;

            Debug.Assert(listedCapabilityStatement != null, nameof(listedCapabilityStatement) + " != null");

            ListedRestComponent listedRestComponent = listedCapabilityStatement.Rest.First();
            if (listedRestComponent.Extension == null)
            {
                listedRestComponent.Extension = new List<Extension>();
            }

            var websocketExtension = new Extension("http://hl7.org/fhir/StructureDefinition/capabilitystatement-websocket", new FhirUri(_subscriptionConfiguration.Websocket.Endpoint));
            _configuredConformanceProvider.ConfigureOptionalCapabilities(configured => configured.Rest.First().Extension.Add(websocketExtension));
            listedRestComponent.Extension.Add(websocketExtension);
        }
    }
}
