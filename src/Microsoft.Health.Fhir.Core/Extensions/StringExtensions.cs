// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.IO;

namespace Microsoft.Health.Fhir.Core.Extensions
{
    internal static class StringExtensions
    {
        private static readonly RecyclableMemoryStreamManager StreamManager = new();

        public static string CompressToGZipBase64(this string data)
        {
            using var stream = StreamManager.GetStream();
            CompressedResourceConverter.WriteCompressedRawResource(stream, data);
            return Convert.ToBase64String(stream.ToArray());
        }

        public static string DecompressGZipBase64(this string base64)
        {
            using var stream = StreamManager.GetStream(Convert.FromBase64String(base64));
            return CompressedResourceConverter.ReadCompressedRawResource(stream);
        }
    }
}
