// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Reflection;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Fhir.SqlServer.Features.Schema
{
    public class SchemaManager
    {
        public SchemaInfo GetSchemaWithTableName(string tableName)
        {
            var tables = typeof(VLatest).Assembly.GetTypes().Where(type => type.DeclaringType == typeof(VLatest) && type.IsSubclassOf(typeof(Table)));
            var tableType = tables.FirstOrDefault(table => string.Equals(table.Name, $"{tableName}Table", System.StringComparison.OrdinalIgnoreCase));
            var fields = tableType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            var columnFields = fields.Where(field => field.FieldType?.IsSubclassOf(typeof(Column)) ?? false);
            var schemaNames = columnFields.Select(field => field.Name).ToList();
            var schemaMapping = schemaNames.Select((name, index) => new { name, index }).ToDictionary(element => element.name, element => element.index);
            return new SchemaInfo(schemaMapping, schemaNames);
        }
    }
}
