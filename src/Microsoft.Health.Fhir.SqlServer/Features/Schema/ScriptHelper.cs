// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Reflection;

namespace Microsoft.Health.Fhir.SqlServer.Features.Schema
{
    public static class ScriptHelper
    {
        public static string GetMigrationScript(int version)
        {
            string resourceName = $"{typeof(SchemaUpgradeRunner).Namespace}.Migrations.{version}.sql";
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static byte[] GetMigrationScriptAsBytes(int version)
        {
            string resourceName = $"{typeof(SchemaUpgradeRunner).Namespace}.Migrations.{version}.sql";
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                var resultByteArray = new byte[stream.Length];
                stream.Read(resultByteArray, 0, resultByteArray.Length);
                return resultByteArray;
            }
        }
    }
}
