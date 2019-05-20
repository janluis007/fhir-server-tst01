// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Operations;
using Microsoft.Health.Fhir.Core.Features.Operations.Export.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Messages.Export
{
    public class CreateExportRequest : IRequest<CreateExportResponse>
    {
        public CreateExportRequest(Uri requestUri, string destinationType, string destinationConnectionString, string resourceType = null)
        {
            EnsureArg.IsNotNull(requestUri, nameof(requestUri));
            EnsureArg.IsNotNullOrWhiteSpace(destinationType, nameof(destinationType));
            EnsureArg.IsNotNullOrWhiteSpace(destinationConnectionString, nameof(destinationConnectionString));

            RequestUri = requestUri;
            DestinationInfo = new DestinationInfo(destinationType, destinationConnectionString);
            ResourceType = resourceType;
        }

        [JsonConstructor]
        protected CreateExportRequest()
        {
        }

        [JsonProperty(JobRecordProperties.RequestUri)]
        public Uri RequestUri { get; private set; }

        // We don't want to store this information in the job record and hence we explicitly
        // ignore it during the serialization process.
        [JsonIgnore]
        public DestinationInfo DestinationInfo { get; }

        [JsonProperty("resourceType")]
        public string ResourceType { get; private set; }
    }
}
