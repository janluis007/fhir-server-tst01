// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;

namespace Microsoft.Health.Fhir.Tests.E2E
{
    public static class Constants
    {
        public const string TestEnvironmentVariableVersionSuffix = "_R5";
        public const string TestEnvironmentVariableVersionSqlSuffix = "_R5_Sql";
        internal const HttpStatusCode IfMatchFailureStatus = HttpStatusCode.PreconditionFailed;
    }
}
