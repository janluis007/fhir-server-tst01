// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    public class JsonToResourceDeserializer : IJsonToResourceDeserializer
    {
        private FhirJsonParser _fhirJsonParser;

        public JsonToResourceDeserializer(FhirJsonParser fhirJsonParser)
        {
            EnsureArg.IsNotNull(fhirJsonParser, nameof(fhirJsonParser));

            _fhirJsonParser = fhirJsonParser;
        }

        public ResourceElement Deserialize(string jsonData)
        {
            EnsureArg.IsNotNullOrWhiteSpace(jsonData, nameof(jsonData));

            Resource resource = _fhirJsonParser.Parse<Resource>(jsonData);
            if (resource.Meta == null)
            {
                resource.Meta = new Meta();
            }

            resource.Meta.LastUpdated = Clock.UtcNow;

            var element = resource.ToTypedElement();
            return new ResourceElement(element);
        }
    }
}
