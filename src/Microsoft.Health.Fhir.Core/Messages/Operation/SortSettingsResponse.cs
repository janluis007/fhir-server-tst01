// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Fhir.Core.Features.Search.Registry;

namespace Microsoft.Health.Fhir.Core.Messages.Operation
{
    public class SortSettingsResponse
    {
        public SortSettingsResponse(Uri uri, SortParameterStatus status)
        {
            Uri = uri;
            Status = status;
        }

        public Uri Uri { get; }

        public SortParameterStatus Status { get; }
    }
}
