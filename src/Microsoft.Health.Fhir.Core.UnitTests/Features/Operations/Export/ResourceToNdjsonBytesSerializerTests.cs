// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Core.Features.Operations.Export;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Tests.Common;
using Xunit;

namespace Microsoft.Health.Fhir.Core.UnitTests.Features.Operations.Export
{
    public class ResourceToNdjsonBytesSerializerTests
    {
        private readonly FhirJsonParser _jsonParser = new FhirJsonParser();
        private readonly FhirXmlParser _xmlParser = new FhirXmlParser();
        private readonly FhirJsonSerializer _jsonSerializer = new FhirJsonSerializer();

        private readonly ResourceToNdjsonBytesSerializer _serializer;

        private readonly Resource _resource;
        private readonly byte[] _expectedBytes;

        public ResourceToNdjsonBytesSerializerTests()
        {
            var resourceDeserializaer = new ResourceDeserializer(
                (ResourceFormat.Json, new Func<string, Resource>(str => _jsonParser.Parse<Resource>(str))),
                (ResourceFormat.Xml, new Func<string, Resource>(str => _xmlParser.Parse<Resource>(str))));

            _serializer = new ResourceToNdjsonBytesSerializer(resourceDeserializaer, _jsonSerializer);

            _resource = Samples.GetDefaultObservation();

            string expectedString = $"{new FhirJsonSerializer().SerializeToString(_resource)}\n";

            _expectedBytes = Encoding.UTF8.GetBytes(expectedString);
        }

        [Fact]
        public void GivenARawResourceInJsonFormat_WhenSerialized_ThenCorrectByteArrayShouldBeProduced()
        {
            RawResource rawResource = new RawResource(
                new FhirJsonSerializer().SerializeToString(_resource),
                Hl7.Fhir.Rest.ResourceFormat.Json);

            byte[] actualBytes = _serializer.Serialize(rawResource);

            Assert.Equal(_expectedBytes, actualBytes);
        }

        [Fact]
        public void GivenARawResourceInXmlFormat_WhenSerialized_ThenCorrectByteArrayShouldBeProduced()
        {
            RawResource rawResource = new RawResource(
                new FhirXmlSerializer().SerializeToString(_resource),
                Hl7.Fhir.Rest.ResourceFormat.Xml);

            byte[] actualBytes = _serializer.Serialize(rawResource);

            Assert.Equal(_expectedBytes, actualBytes);
        }
    }
}
