// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Core.Features.Search.Converters;
using Xunit;
using static Microsoft.Health.Fhir.Tests.Common.Search.SearchValueValidationHelper;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.UnitTests.Features.Search.Converters
{
    public class IdToTokenSearchValueConverterTests : FhirTypedElementToSearchValueConverterTests<IdToTokenSearchValueConverter, Id>
    {
        [Fact]
        public async Task GivenAnIdWithNoValue_WhenConverted_ThenNoSearchValueShouldBeCreated()
        {
            await Test(id => id.Value = null);
        }

        [Fact]
        public async Task GivenAnIdWithValue_WhenConverted_ThenATokenSearchValueShouldBeCreated()
        {
            const string identifier = "id";

            await Test(
                id => id.Value = identifier,
                ValidateToken,
                new Token(null, identifier, null));
        }
    }
}
