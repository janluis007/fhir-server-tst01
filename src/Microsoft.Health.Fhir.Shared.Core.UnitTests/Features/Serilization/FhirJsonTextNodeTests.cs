// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Core.Features.Serialization;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.Tests.Common;
using Xunit;

namespace Microsoft.Health.Fhir.R4.Core.UnitTests.Features.Serilization
{
    public class FhirJsonTextNodeTests
    {
        private string _patientJson = @"{
  ""resourceType"" : ""Patient"",
  ""name"" : [{
    ""id"" : ""f2"",
    ""use"" : ""official"" ,
    ""given"" : [ ""Karen"", ""May"" ],
    ""_given"" : [ null, {""id"" : ""middle""} ],
    ""family"" :  ""Van"",
    ""_family"" : {""id"" : ""a2""}
   }],
  ""text"" : {
    ""status"" : ""generated"" ,
    ""div"" : ""<div xmlns=\""http://www.w3.org/1999/xhtml\""><p>...</p></div>""
  }
}";

        [Fact]
        public void ReadShadowProperty()
        {
            var node = JsonSourceNodeFactory.Parse(_patientJson).ToTypedElement(ModelInfoProvider.StructureDefinitionSummaryProvider);

            var familyName = node.Scalar("Patient.name.family");
            var familyId = node.Scalar("Patient.name.family.id");
            Assert.Equal("Van", familyName);
            Assert.Equal("a2", familyId);

            var middle = node.Scalar("Patient.name.given[1]");
            var middleId = node.Scalar("Patient.name.given[1].id");
            Assert.Equal("May", middle);
            Assert.Equal("middle", middleId);

            var firstName = node.Scalar("Patient.name.given[0]");
            var firstNameId = node.Scalar("Patient.name.given[0].id");
            Assert.Equal("Karen", firstName);
            Assert.Null(firstNameId);
        }

        [Fact]
        public void TestRead()
        {
            ResourceElement resourceElement = Samples.GetDefaultPatient();
            var json = resourceElement.ToJson();

            var node = JsonSourceNodeFactory.Parse(json);

            var el = node.ToTypedElement(ModelInfoProvider.StructureDefinitionSummaryProvider);

            var type = el.InstanceType;
            var id = el.Scalar("Resource.id");

            var name1 = resourceElement.Instance.Scalar("Resource.name[0].family");
            var name = el.Scalar("Resource.name[0].family");

            Assert.Equal("Patient", type);

            var poco = el.ToPoco();
        }

        [Fact]
        public void TestUpdate()
        {
            ResourceElement resourceElement = Samples.GetDefaultPatient();
            resourceElement.UpdateVersion("abc");

            var version = resourceElement.VersionId;
            Assert.Equal("abc", version);
        }
    }
}
