// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import.Models
{
    /// <summary>
    /// Class used to hold data that needs to be returned to the client when the
    /// import job completes. This is a subset of the data present in <see cref="ImportJobRecord"/>.
    /// </summary>
    public class ImportJobResult
    {
        public ImportJobResult(DateTimeOffset transactionTime, Uri requestUri, bool requiresAccessToken, IList<ImportEntryInfo> output, IList<ImportEntryInfo> errors)
        {
            EnsureArg.IsNotDefault<DateTimeOffset>(transactionTime, nameof(transactionTime));
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));

            TransactionTime = transactionTime;
            RequestUri = requestUri;
            RequiresAccessToken = requiresAccessToken;
            Output = output;
            Errors = errors;
        }

        [JsonProperty("transactionTime")]
        public DateTimeOffset TransactionTime { get; }

        [JsonProperty("request")]
        public Uri RequestUri { get; }

        [JsonProperty("requiresAccessToken")]
        public bool RequiresAccessToken { get; }

        [JsonProperty("output")]
        public IList<ImportEntryInfo> Output { get; }

        [JsonProperty("error")]
        public IList<ImportEntryInfo> Errors { get; }
    }
}
