// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Hl7.Fhir.Model;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export
{
    public class GetExportDataResult
    {
        public GetExportDataResult(string continuationToken)
        {
            ContinuationToken = continuationToken;
        }

        public List<Resource> Resources { get; } = new List<Resource>();

        public string ContinuationToken { get; }
    }
}
