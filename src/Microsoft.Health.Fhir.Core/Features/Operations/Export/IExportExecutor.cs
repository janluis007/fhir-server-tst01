// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Health.Fhir.Core.Features.Operations.Export.Models;
using Microsoft.Health.Fhir.Core.Messages.Export;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export
{
    public interface IExportExecutor
    {
        Task<GetExportDataResult> GetExportDataAsync(CreateExportRequest exportRequest, ExportJobProgress jobProgress, int maxCountPerQuery);
    }
}
