// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Health.Fhir.Core.Features.Operations.Import;

namespace Microsoft.Health.Fhir.Operation.AzureBlob.Import
{
    public class AzureBlobImportProvider : IImportProvider
    {
        public string ProviderType { get; } = "azure-blob";

        public async Task<StreamReader> DownloadRangeToStreamReaderAsync(Uri url, long offset, int length, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(url, nameof(url));

            var cloudBlob = new CloudBlob(url);

            var stream = new MemoryStream(length);

            await cloudBlob.DownloadRangeToStreamAsync(stream, offset, length, AccessCondition.GenerateEmptyCondition(), new BlobRequestOptions(), new OperationContext(), cancellationToken);

            stream.Position = 0;

            return new StreamReader(stream);
       }
    }
}
