// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core
{
    /// <summary>
    /// Provides access to R5 FHIR Models and Resources
    /// </summary>
    public partial class VersionSpecificModelInfoProvider
    {
        public FhirSpecification Version => FhirSpecification.R5;
    }
}
