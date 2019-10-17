// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Reflection;
using Microsoft.Health.Fhir.Core.Models;
using Newtonsoft.Json.Schema;

namespace Microsoft.Health.Fhir.Core.Features.Specification
{
    public static class SpecificationLoader
    {
        public static JSchema FhirSchema(FhirSpecification fhirSpecification)
        {
            string manifestName = $"{typeof(SpecificationLoader).Namespace}.{fhirSpecification}.fhir.schema.json";

            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(manifestName))
            using (var reader = new StreamReader(resourceStream))
            {
                return JSchema.Parse(reader.ReadToEnd());
            }
        }
    }
}
