// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Messages.Bundle
{
    public class BundleResponse
    {
        public BundleResponse(ResourceElement bundle)
        {
            EnsureArg.IsNotNull(bundle, nameof(bundle));

            Bundle = bundle;
        }

        public ResourceElement Bundle { get; }
    }
}
