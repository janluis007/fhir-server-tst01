// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;
using Microsoft.Health.Fhir.Core.Messages.Import;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    public class CreateImportRequestHandler : IRequestHandler<CreateImportRequest, CreateImportResponse>
    {
        private IFhirOperationDataStore _fhirOperationDataStore;

        public CreateImportRequestHandler(IFhirOperationDataStore fhirOperationDataStore)
        {
            EnsureArg.IsNotNull(fhirOperationDataStore, nameof(fhirOperationDataStore));

            _fhirOperationDataStore = fhirOperationDataStore;
        }

        public async Task<CreateImportResponse> Handle(CreateImportRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            // TODO: Later we will add some logic here that will check whether a duplicate job already exists
            // and handle it accordingly. For now we just assume all export jobs are unique and create a new one.

            var jobRecord = new ImportJobRecord(request.RequestUri);
            ImportJobOutcome result = await _fhirOperationDataStore.CreateImportJobAsync(jobRecord, cancellationToken);

            // If job creation had failed we would have thrown an exception.
            return new CreateImportResponse(jobRecord.Id);
        }
    }
}
