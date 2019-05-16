// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export.ExportDestinationClient
{
    public interface IExportDestinationClient
    {
        Task CreateNewFileAsync(string fileName);

        Task CommitAsync(Dictionary<string, List<Resource>> fileNameToResourcesMapping);

        Task ConnectAsync(string destinationConnectionString);
    }
}
