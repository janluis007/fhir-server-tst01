// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Fhir.Core.Features.Subscriptions;

namespace Microsoft.Health.Fhir.Subscription.Webhook.Features.Subscriptions
{
    public class QueueWebhookSubscriptionNotifier : IWebsocketSubscriptionNotifier
    {
        private readonly CloudQueue _queue;

        public QueueWebhookSubscriptionNotifier(IConfiguration configuration)
        {
            var storageConnectionString = configuration["Azure:Storage:ConnectionString"];

            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            _queue = queueClient.GetQueueReference("webhook");

            // Create the queue if it doesn't already exist
            _queue.CreateIfNotExists();
        }

        public async Task Ping(Hl7.Fhir.Model.Subscription subscription)
        {
            // Create a message and add it to the queue.
            var message = new CloudQueueMessage(subscription.Id);
            await _queue.AddMessageAsync(message);
        }
    }
}
