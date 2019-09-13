// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;
using Microsoft.Health.Fhir.Core.Features.Persistence;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    /// <summary>
    /// Provides mechanism to create a new import job task.
    /// </summary>
    public interface IImportJobTaskFactory
    {
        /// <summary>
        /// Creates a new export job task.
        /// </summary>
        /// <param name="importJobRecord">The job record.</param>
        /// <param name="weakETag">The version ETag associated with the job record.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the export job.</returns>
        Task Create(ImportJobRecord importJobRecord, WeakETag weakETag, CancellationToken cancellationToken);
    }
}
