// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Fhir.Core.Models
{
    internal interface IResourceElementPropertyAccessor
    {
        string Id { get; }

        string LastUpdated { get; }

        string VersionId { get; }

        string InstanceType { get; }
    }
}
