// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Core.Features.Security
{
    public class IdentityProvider
    {
        public IdentityProvider()
        {
        }

        public IdentityProvider(string name, string authority, string audience)
        {
            Name = name;
            Authority = authority;
            Audience = audience;
        }

        public string Name { get; set; }

        public string Authority { get; set; }

        public string Audience { get; set; }

        public string Version { get; set; }
    }
}
