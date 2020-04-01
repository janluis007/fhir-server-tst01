// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Health.Fhir.Core.Features.Operations.Patch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Api.Features.Patch
{
    public class AspNetPatchProcessor : IPatchProcessor
    {
        public void Patch(object source, IEnumerable<PatchOperation> operations)
        {
            EnsureArg.IsNotNull(source, nameof(source));
            EnsureArg.IsNotNull(operations, nameof(operations));

            // Convert to ASP.NET JsonPatchDocument
            JsonPatchDocument patchDocument =
                JsonConvert.DeserializeObject<JsonPatchDocument>(JsonConvert.SerializeObject(operations));

            patchDocument.ApplyTo(source);
        }
    }
}
