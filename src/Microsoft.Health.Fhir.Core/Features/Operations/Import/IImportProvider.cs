// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    public interface IImportProvider
    {
        string ProviderType { get; }

        Task<StreamReader> DownloadRangeToStreamReaderAsync(Uri url, long offset, int length, CancellationToken cancellationToken);
    }
}
