// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export.ExportDestinationClient
{
    public interface IExportDestinationClient
    {
        string DestinationType { get; }

        Task ConnectAsync(string destinationConnectionString, CancellationToken cancellationToken);

        Task<Uri> CreateFileAsync(string fileName, CancellationToken cancellationToken);

        Task WriteFilePartAsync(Uri fileUri, uint partId, byte[] bytes, CancellationToken cancellationToken);

        Task CommitAsync(CancellationToken cancellationToken);
    }
}
