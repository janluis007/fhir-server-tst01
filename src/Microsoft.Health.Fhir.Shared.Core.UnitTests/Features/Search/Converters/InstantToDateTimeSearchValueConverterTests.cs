// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Core.Features.Search.Converters;
using Xunit;
using static Microsoft.Health.Fhir.Tests.Common.Search.SearchValueValidationHelper;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.UnitTests.Features.Search.Converters
{
    public class InstantToDateTimeSearchValueConverterTests : FhirTypedElementToSearchValueConverterTests<InstantToDateTimeSearchValueConverter, Instant>
    {
        [Fact]
        public async Task GivenAInstantWithNoValue_WhenConverted_ThenNoSearchValueShouldBeCreated()
        {
            await Test(i => i.Value = null);
        }

        [Fact]
        public async Task GivenAInstantWithValue_WhenConverted_ThenADateTimeSearchValueShouldBeCreated()
        {
            var dateTimeOffset = new DateTimeOffset(2018, 01, 20, 14, 34, 24, TimeSpan.FromMinutes(60));

            // The date time will be stored as UTC.
            await Test(
                i => i.Value = dateTimeOffset,
                ValidateDateTime,
                "2018-01-20T13:34:24.0000000-00:00");
        }
    }
}
