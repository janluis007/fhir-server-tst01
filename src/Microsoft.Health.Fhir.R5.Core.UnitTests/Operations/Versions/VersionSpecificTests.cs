﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Core;
using Microsoft.Health.Fhir.Core.Models;
using Xunit;

namespace Microsoft.Health.Fhir.R5.Core.UnitTests.Operations.Versions
{
    /// <summary>
    /// Provides R5 specific tests.
    /// </summary>
    public class VersionSpecificTests
    {
        private readonly IModelInfoProvider _provider;

        public VersionSpecificTests()
        {
            _provider = new VersionSpecificModelInfoProvider();
        }

        [Fact]
        public void GivenR5Server_WhenSupportedVersionIsRequested_ThenCorrectVersionShouldBeReturned()
        {
            var version = _provider.SupportedVersion.ToString();

            Assert.Equal("5.0.0-snapshot1", version);
        }
    }
}
