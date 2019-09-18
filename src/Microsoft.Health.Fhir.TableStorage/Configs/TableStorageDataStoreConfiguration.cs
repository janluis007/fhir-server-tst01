// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.TableStorage.Configs
{
    public class TableStorageDataStoreConfiguration
    {
        public string ConnectionString { get; set; }

        public string TableName { get; set; } = "fhirResources";

        public bool AllowTableScans { get; set; } = true;

        /// <summary>
        /// For example if there are 10 string fields in a Patient
        /// resource the first 5 will be indexed and searchable.
        ///
        /// There is a max of 256 columns in TableStorage, this can ensure
        /// other types are also indexed.
        /// </summary>
        public int MaxIndexCombinationsPerType { get; set; } = 10;
    }
}
