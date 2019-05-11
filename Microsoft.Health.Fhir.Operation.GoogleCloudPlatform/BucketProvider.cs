// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Microsoft.Health.Fhir.Core.Features.Operations.Import;

namespace Microsoft.Health.Fhir.Operation.GoogleCloudPlatform
{
    public class BucketProvider : IImportProvider
    {
        public string ProviderType { get; } = "gcp-bucket";

        public async Task<StreamReader> DownloadRangeToStreamReaderAsync(Uri url, long offset, int length, CancellationToken cancellationToken)
        {
            var storageClient = StorageClient.CreateUnauthenticated();

            string bucket = null;

            switch (url.Scheme.ToUpperInvariant())
            {
                case "GC":
                    bucket = url.Authority;
                    break;

                case "HTTP":
                case "HTTPS":
                    bucket = url.Segments[url.Segments.Length - 2].TrimEnd('\\');
                    break;
            }

            string objectName = url.Segments.Last();

            var options = new DownloadObjectOptions()
            {
                Range = new System.Net.Http.Headers.RangeHeaderValue(offset, offset + length - 1),
            };

            var stream = new MemoryStream();

            await storageClient.DownloadObjectAsync(bucket, objectName, stream, options, cancellationToken);

            stream.Position = 0;

            return new StreamReader(stream);
        }
    }
}
