// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Operations;
using Microsoft.Health.Fhir.Core.Messages.Operation;

namespace Microsoft.Health.Fhir.SqlServer.Features.Operations.SortSettings
{
    public class SortSettingsHandler : IRequestHandler<UpdateSortSettingsRequest, SortSettingsResponse>, IRequestHandler<GetSortSettingsRequest, SortSettingsResponse>
    {
        public Task<SortSettingsResponse> Handle(UpdateSortSettingsRequest request, CancellationToken cancellationToken)
        {
            throw new OperationNotImplementedException(Resources.OperationNotImplemented);
        }

        public Task<SortSettingsResponse> Handle(GetSortSettingsRequest request, CancellationToken cancellationToken)
        {
            throw new OperationNotImplementedException(Resources.OperationNotImplemented);
        }
    }
}
