// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Core;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.Tests.Common;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Api.UnitTests.Performance
{
    [MemoryDiagnoser]
    [InProcess]
    public class SerializationPerformanceTests
    {
        private static readonly ResourceWrapper Wrapper;
        private static readonly Resource Observation;
        private static readonly RawResourceFactory RawResourceFactory;
        private static readonly ResourceDeserializer ResourceDeserializer;
        private static readonly string ObservationJson;
        private static readonly FhirJsonNode ObservationJsonNode;
        private static readonly ITypedElement ObservationTypedElement;

        static SerializationPerformanceTests()
        {
            ModelInfoProvider.SetProvider(new VersionSpecificModelInfoProvider());
            RawResourceFactory = new RawResourceFactory(new FhirJsonSerializer());

            Observation = Samples.GetDefaultObservation().ToPoco();
            Observation.Id = "id1";

            var resourceElement = Observation.ToResourceElement();
            Wrapper = new ResourceWrapper(resourceElement, RawResourceFactory.Create(resourceElement), new ResourceRequest(new Uri("http://fhir"), HttpMethod.Post), false, null, null, null);

            ResourceDeserializer = Deserializers.ResourceDeserializer;

            ObservationJson = RawResourceFactory.Create(Observation.ToResourceElement()).Data;
            ObservationJsonNode = (FhirJsonNode)FhirJsonNode.Parse(ObservationJson);
            ObservationTypedElement = FhirJsonNode.Parse(ObservationJson).ToTypedElement(ModelInfoProvider.Instance.StructureDefinitionSummaryProvider);
        }

        [Benchmark(Baseline = true)]
        public void SerializingWithNewtonSoft()
        {
            JsonConvert.SerializeObject(Observation);
        }

        [Benchmark]
        public void DeserializingWithNewtonSoft()
        {
            JsonConvert.DeserializeObject(Wrapper.RawResource.Data);
        }

        [Benchmark]
        public void SerializingWithTextJson()
        {
            System.Text.Json.JsonSerializer.Serialize(Wrapper.RawResource.Data);
        }

        [Benchmark]
        public void DeserializingWithTextJson()
        {
            JsonDocument.Parse(Wrapper.RawResource.Data);
        }

        [Benchmark]
        public void SerializingWithFhirSdk()
        {
            RawResourceFactory.Create(Observation.ToResourceElement());
        }

        [Benchmark]
        public void DeserializingWithFhirSdk()
        {
            ResourceDeserializer.Deserialize(Wrapper);
        }

        [Benchmark]
        public void SerializingWithITypedElement()
        {
            ObservationJsonNode.ToJson();
        }

        [Benchmark]
        public void SerializingWithFhirJsonNode()
        {
            ObservationJsonNode.ToJson();
        }

        [Benchmark]
        public void DeserializingWithFhirJsonNode()
        {
            FhirJsonNode.Parse(ObservationJson);
        }
    }
}
