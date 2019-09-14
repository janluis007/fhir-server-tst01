// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;
using Microsoft.Health.Fhir.Core.Messages.Export;
using Microsoft.Health.Fhir.Core.Messages.Import;

namespace Microsoft.Health.Fhir.Core.Extensions
{
    public static class ExportMediatorExtensions
    {
        public static async Task<CreateExportResponse> ExportAsync(
            this IMediator mediator,
            Uri requestUri,
            string destinationType,
            string destinationConnectionString,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));
            EnsureArg.IsNotNullOrWhiteSpace(destinationType, nameof(destinationType));
            EnsureArg.IsNotNullOrWhiteSpace(destinationConnectionString, nameof(destinationConnectionString));

            var request = new CreateExportRequest(requestUri, destinationType, destinationConnectionString);

            var response = await mediator.Send(request, cancellationToken);
            return response;
        }

        public static async Task<GetExportResponse> GetExportStatusAsync(this IMediator mediator, Uri requestUri, string jobId, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));
            EnsureArg.IsNotNullOrWhiteSpace(jobId, nameof(jobId));

            var request = new GetExportRequest(requestUri, jobId);

            GetExportResponse response = await mediator.Send(request, cancellationToken);
            return response;
        }

        public static async Task<CancelExportResponse> CancelExportAsync(this IMediator mediator, string jobId, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNullOrWhiteSpace(jobId, nameof(jobId));

            var request = new CancelExportRequest(jobId);

            return await mediator.Send(request, cancellationToken);
        }

        public static async Task<CreateImportResponse> ImportAsync(this IMediator mediator, Uri requestUri, ImportRequest importRequest, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));
            EnsureArg.IsNotNull(importRequest, nameof(importRequest));

            var request = new CreateImportRequest(requestUri, importRequest);

            var response = await mediator.Send(request, cancellationToken);

            return response;
        }

        public static async Task<GetImportResponse> GetImportStatusAsync(this IMediator mediator, Uri requestUri, string jobId, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));
            EnsureArg.IsNotNullOrWhiteSpace(jobId, nameof(jobId));

            var request = new GetImportRequest(requestUri, jobId);

            GetImportResponse response = await mediator.Send(request, cancellationToken);
            return response;
        }
    }
}
