// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Hl7.Fhir.Model;
using MediatR;
using Microsoft.Health.Fhir.Core.Messages.Create;
using Microsoft.Health.Fhir.Core.Messages.Upsert;
using Microsoft.Health.Fhir.Core.Notifications;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.Features.Resources.Create
{
    /// <summary>
    /// Handles the version specific operations
    /// </summary>
    public partial class CreateResourceHandler : BaseResourceHandler, IRequestHandler<CreateResourceRequest, UpsertResourceResponse>
    {
        public async Task HandleVersionSpecificOperations(Resource message, CancellationToken cancellationToken)
        {
            if (message is Subscription subscription)
            {
                await _mediator.Publish(new UpsertSubscriptionNotification(subscription), cancellationToken);
            }
        }
    }
}
