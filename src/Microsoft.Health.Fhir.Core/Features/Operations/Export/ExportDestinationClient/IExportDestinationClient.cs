// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export.ExportDestinationClient
{
    public interface IExportDestinationClient
    {
        Task<Uri> CreateNewFileAsync(string fileName);

        Task CommitAsync(Dictionary<Uri, List<Resource>> fileNameToResourcesMapping);

        Task ConnectAsync(string destinationConnectionString);
    }
}
