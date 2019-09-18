// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Fhir.Tests.Common.FixtureParameters
{
    [Flags]
    public enum DataStore
    {
        CosmosDb = 1 << 0,

        SqlServer = 1 << 1,

        TableStorage = 1 << 2,

        All = CosmosDb | SqlServer | TableStorage,

        CosmosAndSql = CosmosDb | SqlServer,
    }
}
