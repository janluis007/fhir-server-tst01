// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.Core.Serialization;
using Microsoft.Health.Fhir.Tests.Common;
using Xunit;

namespace Microsoft.Health.Fhir.R4.Core.UnitTests.Features.Serilization
{
    public class FhirJsonTextNodeTests
    {
        [Fact]
        public void TestRead()
        {
            ResourceElement resourceElement = Samples.GetDefaultPatient();
            var json = resourceElement.ToJson();

            var node = FhirJsonTextNode.Parse(json);

            var el = node.ToTypedElement(ModelInfoProvider.StructureDefinitionSummaryProvider);

            var type = el.InstanceType;
            var id = el.Scalar("Resource.id");

            var name1 = resourceElement.Instance.Scalar("Resource.name[0].family");
            var name = el.Scalar("Resource.name[0].family");

            Assert.Equal("Patient", type);
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
