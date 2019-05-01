// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using EnsureThat;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;

namespace Microsoft.Health.Fhir.Api.Features.ActionResults
{
    /// <summary>
    /// Used to return the result of an import operation.
    /// </summary>
    public class ImportResult : BaseActionResult<ImportJobResult>
    {
        public ImportResult(HttpStatusCode statusCode)
            : base(null, statusCode)
        {
        }

        public ImportResult(ImportJobResult jobResult, HttpStatusCode statusCode)
            : base(jobResult, statusCode)
        {
            EnsureArg.IsNotNull(jobResult, nameof(jobResult));
        }

        /// <summary>
        /// Creates an ExportResult with HttpStatusCode Accepted.
        /// </summary>
        public static ImportResult Accepted()
        {
            return new ImportResult(HttpStatusCode.Accepted);
        }

        /// <summary>
        /// Creates an ExportResult with HttpStatusCode Ok.
        /// </summary>
        /// <param name="jobResult">The job payload that must be returned as part of the ExportResult.</param>
        public static ImportResult Ok(ImportJobResult jobResult)
        {
            return new ImportResult(jobResult, HttpStatusCode.OK);
        }
    }
}
