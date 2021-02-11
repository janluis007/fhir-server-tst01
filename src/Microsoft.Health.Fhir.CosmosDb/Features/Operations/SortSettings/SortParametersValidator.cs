// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;
using Microsoft.Health.Fhir.Core.Features.Search.Registry;
using Microsoft.Health.Fhir.Core.Models;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Operations.SortSettings
{
    public class SortParametersValidator : IPropertyValidator
    {
        public PropertyValidatorOptions Options { get; set; } = new PropertyValidatorOptions();

        public bool ShouldValidateAsynchronously(IValidationContext context) => true;

        public IEnumerable<ValidationFailure> Validate(PropertyValidatorContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            if (context.PropertyValue is ResourceElement resourceElement)
            {
                if (!string.Equals(KnownResourceTypes.Parameters, resourceElement.InstanceType, StringComparison.Ordinal))
                {
                    yield return new ValidationFailure(KnownResourceTypes.Resource, "Must be a Parameters resource.");
                    yield break;
                }

                var fhirUri = resourceElement.Scalar<string>(KnownSortParametersFhirPaths.UriValue);
                var setting = resourceElement.Scalar<string>(KnownSortParametersFhirPaths.EnabledValue);

                if (string.IsNullOrEmpty(fhirUri) || !Uri.TryCreate(fhirUri, UriKind.Absolute, out Uri _))
                {
                    yield return new ValidationFailure(KnownSortParametersFhirPaths.UriValue, Core.Resources.SortParameterUriNotValid);
                }

                if (string.IsNullOrEmpty(setting) || !Enum.TryParse(typeof(SortParameterStatus), setting, true, out _))
                {
                    yield return new ValidationFailure(KnownSortParametersFhirPaths.EnabledValue, Core.Resources.SortParameterStatusNotValid);
                }
            }
        }

        public Task<IEnumerable<ValidationFailure>> ValidateAsync(PropertyValidatorContext context, CancellationToken cancellation)
        {
            return Task.FromResult(Validate(context));
        }
    }
}
