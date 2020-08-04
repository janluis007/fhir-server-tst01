// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Core.Exceptions;
using Microsoft.Health.Core.Models;

namespace Microsoft.Health.Fhir.Core.Exceptions
{
    public class UnsupportedConfigurationException : HealthException
    {
        public UnsupportedConfigurationException(string message, OperationOutcomeIssue[] issues = null)
            : base(message, issues)
        {
        }
    }
}
