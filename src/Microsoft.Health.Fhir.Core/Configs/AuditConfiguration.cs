// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Core.Configs
{
    public class AuditConfiguration : Microsoft.Health.Core.Configs.AuditConfiguration
    {
        public AuditConfiguration()
            : base("X-MS-AZUREFHIR-AUDIT-")
        {
        }
    }
}
