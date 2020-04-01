// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Api.Features.Patch.FhirPatch
{
    public interface IPathProvider
    {
        IEnumerable<JToken> Select(JToken source, string path);

        public void Set(JToken source, string path, JToken value);
    }
}
