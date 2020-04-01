// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Api.Features.Patch.FhirPatch
{
    public class JsonPath : IPathProvider
    {
        public IEnumerable<JToken> Select(JToken source, string path)
        {
            EnsureArg.IsNotNull(source, nameof(source));
            EnsureArg.IsNotNull(path, nameof(path));

            return source.SelectTokens(path);
        }

        public void Set(JToken source, string path, JToken value)
        {
            EnsureArg.IsNotNull(source, nameof(source));
            EnsureArg.IsNotNull(path, nameof(path));
            EnsureArg.IsNotNull(value, nameof(value));

            foreach (JToken item in source.SelectTokens(path))
            {
                item.Replace(value);
            }
        }
    }
}
