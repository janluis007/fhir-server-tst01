// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Fhir.SqlServer.Features.Schema.Model
{
    public class SchemaInfo
    {
        public SchemaInfo(Dictionary<string, int> schemaMapping, List<string> schemaColumns)
        {
            SchemaMapping = schemaMapping;
            SchemaColumns = schemaColumns;
        }

        public Dictionary<string, int> SchemaMapping { get; }

        public List<string> SchemaColumns { get; }
    }
}
