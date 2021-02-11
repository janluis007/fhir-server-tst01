// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.CosmosDb.Features.Operations.SortSettings
{
    internal sealed class KnownSortParametersFhirPaths
    {
        public const string ParameterUriName = "uri";

        public const string ParameterStatusName = "status";

        public const string UriValue = "Parameters.parameter.where(name = '" + ParameterUriName + "').value";

        public const string EnabledValue = "Parameters.parameter.where(name = '" + ParameterStatusName + "').value";
    }
}
