// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Health.Fhir.Core.Serialization.SourceNodes
{
    public interface IExtensionData
    {
        IDictionary<string, JsonElement> ExtensionData { get; }
    }
}
