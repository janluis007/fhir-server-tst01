// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.FhirPath;
using Microsoft.Health.Core;
using Microsoft.Health.Fhir.Core;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.Tests.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Api.UnitTests.Performance
{
    [MemoryDiagnoser]
    [InProcess]
    public class UpdatePropertiesPerformanceTests
    {
        private static readonly string ObservationJson;
        private static readonly FhirJsonParser _parser;
        private static FhirJsonSerializer _serializer;

        static UpdatePropertiesPerformanceTests()
        {
            ModelInfoProvider.SetProvider(new VersionSpecificModelInfoProvider());
            ObservationJson = Samples.GetDefaultObservation().ToPoco().ToJson();
            _parser = new FhirJsonParser();
            _serializer = new FhirJsonSerializer();
        }

        [Benchmark(Baseline = true)]
        public void UpdatePropertiesPocos()
        {
            var poco = _parser.Parse<Resource>(ObservationJson);
            poco.Id = "id1";
            poco.Meta = new Meta
            {
                VersionId = "1",
                LastUpdated = Clock.UtcNow,
            };
            _serializer.SerializeToString(poco);
        }

        [Benchmark]
        public void ReadPropertiesPocos()
        {
            var poco = _parser.Parse<Resource>(ObservationJson);
            var id = poco.Id;
            var version = poco.VersionId;
            var lastUpdated = poco.Meta?.LastUpdated;
        }

        [Benchmark]
        public void UpdatePropertiesFhirJsonNode()
        {
            var obj = (FhirJsonNode)FhirJsonNode.Parse(ObservationJson);
            obj.JsonObject.Merge(JObject.FromObject(new { id = "id1" }));
            obj.JsonObject.Merge(JObject.FromObject(new { meta = new { versionId = "1" } }));
            obj.JsonObject.Merge(JObject.FromObject(new { meta = new { lastUpdated = Clock.UtcNow.ToString("o") } }));
            obj.ToJson();
        }

        [Benchmark]
        public void ReadPropertiesFhirJsonNode()
        {
            var obj = FhirJsonNode.Parse(ObservationJson).ToTypedElement(ModelInfoProvider.StructureDefinitionSummaryProvider);
            obj.Scalar("Resource.id");
            obj.Scalar("Resource.meta.versionId");
            obj.Scalar("Resource.meta.lastUpdated");
        }
    }
}
