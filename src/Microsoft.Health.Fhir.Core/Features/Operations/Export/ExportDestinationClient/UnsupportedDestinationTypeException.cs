// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Core.Exceptions;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export.ExportDestinationClient
{
    public class UnsupportedDestinationTypeException : FhirException
    {
        public UnsupportedDestinationTypeException(string destinationType)
            : base(string.Format(Core.Resources.UnsupportedDestinationTypeMessage, destinationType))
        {
            Issues.Add(new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Code = OperationOutcome.IssueType.NotFound,
                Diagnostics = Message,
            });
        }
    }
}
