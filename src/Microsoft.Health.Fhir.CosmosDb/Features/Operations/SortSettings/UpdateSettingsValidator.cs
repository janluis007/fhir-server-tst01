// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Health.Fhir.Core.Messages.Operation;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Operations.SortSettings
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Follows validator naming convention.")]
    public class UpdateSettingsValidator : AbstractValidator<UpdateSortSettingsRequest>
    {
        public UpdateSettingsValidator()
        {
            RuleFor(x => x.ResourceElement)
                .SetValidator(new SortParametersValidator());
        }
    }
}
