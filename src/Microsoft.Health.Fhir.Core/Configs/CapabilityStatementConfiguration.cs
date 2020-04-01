// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Fhir.Core.Features.Operations.Patch;

namespace Microsoft.Health.Fhir.Core.Configs
{
    public class CapabilityStatementConfiguration
    {
        public ICollection<PatchOperation> Operations { get; } = new List<PatchOperation>();
    }
}
