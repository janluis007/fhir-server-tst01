// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using BenchmarkDotNet.Attributes;
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
        public void SerializingWithFhirJsonNode()
        {
            FhirJsonNode.Parse(ObservationJson);
        }

        [Benchmark]
        public void DeserializingWithFhirJsonNode()
        {
            ObservationJsonNode.ToJson();
        }
    }
}
