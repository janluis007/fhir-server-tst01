// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Fhir.Api.Features.Security
{
    public class IssuerSchemeMapper
    {
        public Dictionary<string, string> AuthorityToSchemeMap { get; } = new Dictionary<string, string>();
    }
}
