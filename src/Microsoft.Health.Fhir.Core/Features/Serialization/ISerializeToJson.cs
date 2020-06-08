// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Health.Fhir.Core.Features.Serialization
{
    public interface ISerializeToJson
    {
        string SerializeToJson(bool writeIndented = false);

        Task SerializeToJson(Stream stream, bool writeIndented = false);
    }
}
