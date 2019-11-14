// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;
using Microsoft.Health.Fhir.Core.Messages.Import;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    public class GetImportRequestHandler : IRequestHandler<GetImportRequest, GetImportResponse>
    {
        private IFhirOperationDataStore _fhirOperationDataStore;

        public GetImportRequestHandler(IFhirOperationDataStore fhirOperationDataStore)
        {
            EnsureArg.IsNotNull(fhirOperationDataStore, nameof(fhirOperationDataStore));

            _fhirOperationDataStore = fhirOperationDataStore;
        }

        public async Task<GetImportResponse> Handle(GetImportRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            ImportJobOutcome outcome = await _fhirOperationDataStore.GetImportJobAsync(request.JobId, cancellationToken);

            // We have an existing job. We will determine the response based on the status of the export operation.
            GetImportResponse exportResponse;
            if (outcome.JobRecord.Status == OperationStatus.Completed)
            {
                var jobResult = new ImportJobResult(
                    outcome.JobRecord.QueuedTime,
                    outcome.JobRecord.RequestUri,
                    requiresAccessToken: false,
                    outcome.JobRecord.Progress.Select((progress, index) => new ImportEntryInfo(outcome.JobRecord.Request.Input[index].Type, progress.Count)).ToArray(),
                    null);

                exportResponse = new GetImportResponse(HttpStatusCode.OK, jobResult);
            }
            else if (outcome.JobRecord.Status == OperationStatus.Failed || outcome.JobRecord.Status == OperationStatus.Canceled)
            {
                HttpStatusCode failureStatusCode = HttpStatusCode.InternalServerError;

                if (outcome.JobRecord.Errors != null && outcome.JobRecord.Errors.Any())
                {
                    throw new OperationFailedException(
                        string.Format(Resources.OperationFailed, OperationsConstants.Import, outcome.JobRecord.Errors), failureStatusCode);
                }
                else
                {
                    throw new OperationFailedException(
                        string.Format(Resources.OperationFailed, OperationsConstants.Import, "Failed to process the job."), failureStatusCode);
                }
            }
            else
            {
                exportResponse = new GetImportResponse(HttpStatusCode.Accepted);
            }

            return exportResponse;
        }
    }
}
