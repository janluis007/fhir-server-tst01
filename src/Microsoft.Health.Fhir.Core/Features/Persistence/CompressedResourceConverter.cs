// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.Core.Features.Operations;

namespace Microsoft.Health.Fhir.Core.Features.Persistence
{
    internal class CompressedResourceConverter : ICompressedRawResourceConverter
    {
        internal static readonly Encoding LegacyResourceEncoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);

        internal static readonly Encoding ResourceEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        public static string ReadCompressedRawResource(Stream compressedResourceStream)
        {
            using var gzipStream = new GZipStream(compressedResourceStream, CompressionMode.Decompress, leaveOpen: true);

            // The current resource encoding uses byte-order marks. The legacy encoding does not, so we provide is as the fallback encoding
            // when there is no BOM
            using var reader = new StreamReader(gzipStream, LegacyResourceEncoding, detectEncodingFromByteOrderMarks: true);

            return reader.ReadToEnd();
        }

        public static void WriteCompressedRawResource(Stream outputStream, string rawResource)
        {
            using var gzipStream = new GZipStream(outputStream, CompressionLevel.Fastest, leaveOpen: true);
            using var writer = new StreamWriter(gzipStream, ResourceEncoding);
            writer.Write(rawResource);
            writer.Flush();
        }

        void ICompressedRawResourceConverter.WriteCompressedRawResource(Stream outputStream, string rawResource)
        {
            WriteCompressedRawResource(outputStream, rawResource);
        }

        Task<string> ICompressedRawResourceConverter.ReadCompressedRawResource(Stream compressedResourceStream)
        {
            return Task.FromResult(ReadCompressedRawResource(compressedResourceStream));
        }
    }
}
