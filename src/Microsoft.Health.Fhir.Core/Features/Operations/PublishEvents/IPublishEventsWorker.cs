// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Fhir.Core.Features.Operations.PublishEvents
{
    /// <summary>
    /// Abstraction for Publish Events Worker.
    /// </summary>
    public interface IPublishEventsWorker
    {
        /// <summary>
        /// Async Execute method that performs some work.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
