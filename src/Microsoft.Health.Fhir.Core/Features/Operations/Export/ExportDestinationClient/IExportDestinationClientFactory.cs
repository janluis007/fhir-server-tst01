// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export.ExportDestinationClient
{
    public interface IExportDestinationClientFactory
    {
        IExportDestinationClient Create(string destinationType);

        bool IsSupportedDestinationType(string destinationType);
    }
}
