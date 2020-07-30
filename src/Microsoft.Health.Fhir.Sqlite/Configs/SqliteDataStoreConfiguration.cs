// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Sqlite.Configs
{
    public class SqliteDataStoreConfiguration
    {
        public string ConnectionString { get; set; }

        public string DatabaseFileName { get; set; }
    }
}
