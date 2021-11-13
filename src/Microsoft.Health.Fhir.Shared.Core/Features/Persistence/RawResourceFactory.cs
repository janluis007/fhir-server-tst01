// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Features.Persistence
{
    /// <summary>
    /// Provides a mechanism to create a <see cref="RawResource"/>
    /// </summary>
    public class RawResourceFactory : IRawResourceFactory
    {
        private readonly FhirJsonSerializer _fhirJsonSerializer;
        private readonly ILogger<RawResourceFactory> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RawResourceFactory"/> class.
        /// </summary>
        /// <param name="fhirJsonSerializer">The FhirJsonSerializer to use for serializing the resource.</param>
        /// <param name="logger">Logger.</param>
        public RawResourceFactory(FhirJsonSerializer fhirJsonSerializer, ILogger<RawResourceFactory> logger)
        {
            EnsureArg.IsNotNull(fhirJsonSerializer, nameof(fhirJsonSerializer));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _fhirJsonSerializer = fhirJsonSerializer;
            _logger = logger;
        }

        /// <inheritdoc />
        public RawResource Create(ResourceElement resource, bool keepMeta)
        {
            EnsureArg.IsNotNull(resource, nameof(resource));

            var poco = resource.ToPoco<Resource>();

            poco.Meta ??= new Meta();
            var versionId = poco.Meta.VersionId;

            try
            {
                // Clear meta version if keepMeta is false since this is set based on generated values when saving the resource
                if (!keepMeta)
                {
                    poco.Meta.VersionId = null;
                }
                else
                {
                    // Assume it's 1, though it may get changed by the database.
                    poco.Meta.VersionId = "1";
                }

                string serializeToString = _fhirJsonSerializer.SerializeToString(poco);
                var base64 = serializeToString.CompressToGZipBase64();

                if (base64.Length < serializeToString.Length)
                {
                    _logger.LogInformation(
                        "Resource size: {uncompressedSize}, Compressed: {compressedSize}, Saved: {savedSize}%",
                        serializeToString.Length,
                        base64.Length,
                        Math.Round((1 - (base64.Length / (double)serializeToString.Length)) * 100, 2));

                    return new RawResource(base64, FhirResourceFormat.CompressedJson, keepMeta)
                    {
                        CompressedData = base64,
                    };
                }

                return new RawResource(serializeToString, FhirResourceFormat.Json, keepMeta)
                {
                    CompressedData = base64,
                };
            }
            finally
            {
                if (!keepMeta)
                {
                    poco.Meta.VersionId = versionId;
                }
            }
        }
    }
}
