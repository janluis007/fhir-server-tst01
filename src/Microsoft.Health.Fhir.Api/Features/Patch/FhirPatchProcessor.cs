// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Fhir.Api.Features.Patch.FhirPatch;
using Microsoft.Health.Fhir.Core.Features.Operations.Patch;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Api.Features.Patch
{
    public class FhirPatchProcessor : IPatchProcessor
    {
        private readonly IPathProvider _pathProvider;

        public FhirPatchProcessor(IPathProvider pathProvider)
        {
            EnsureArg.IsNotNull(pathProvider, nameof(pathProvider));

            _pathProvider = pathProvider;
        }

        public void Patch(object source, IEnumerable<PatchOperation> operations)
        {
            EnsureArg.IsNotNull(source, nameof(source));
            EnsureArg.IsNotNull(operations, nameof(operations));

            var token = (JToken)source;

            foreach (var op in operations)
            {
                var val = JToken.FromObject(op.Value);

                switch (op.Op)
                {
                    case "add":
                    case "replace":
                        _pathProvider.Set(token, op.Path, val);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
