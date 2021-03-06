// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export.Models
{
    /// <summary>
    /// Class used to hold data that needs to be returned to the client when the
    /// export job completes. This is a subset of the data present in <see cref="ExportJobRecord"/>.
    /// </summary>
    public class ExportJobResult
    {
        public ExportJobResult(DateTimeOffset transactionTime, Uri requestUri, bool requiresAccessToken, IList<ExportOutputResponse> output, IList<ExportOutputResponse> errors, IList<Core.Models.OperationOutcomeIssue> issues)
        {
            EnsureArg.IsNotDefault<DateTimeOffset>(transactionTime, nameof(transactionTime));
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));
            EnsureArg.IsNotNull(output, nameof(output));
            EnsureArg.IsNotNull(errors, nameof(errors));

            TransactionTime = transactionTime;
            RequestUri = requestUri;
            RequiresAccessToken = requiresAccessToken;
            Output = output;
            Error = errors;
            Issues = issues;
        }

        [JsonConstructor]
        private ExportJobResult()
        {
        }

        [JsonProperty("transactionTime")]
        public DateTimeOffset TransactionTime { get; private set; }

        [JsonProperty("request")]
        public Uri RequestUri { get; private set; }

        [JsonProperty("requiresAccessToken")]
        public bool RequiresAccessToken { get; private set; }

        [JsonProperty("output")]
        public IList<ExportOutputResponse> Output { get; private set; }

        [JsonProperty("error")]
        public IList<ExportOutputResponse> Error { get; private set; }

        [JsonProperty("issues")]
        public IList<Microsoft.Health.Fhir.Core.Models.OperationOutcomeIssue> Issues { get; private set; }
    }
}
