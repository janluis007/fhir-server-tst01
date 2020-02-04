// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Validation;

namespace Microsoft.Health.Fhir.Api.Features.Validate
{
    public class ProfileValidator
    {
        private Validator _validator;

        public ProfileValidator(ProfileResolver resolver)
        {
            var settings = new ValidationSettings()
            {
                ResourceResolver = resolver,
            };
            _validator = new Validator(settings);
        }

        public OperationOutcome ValidateWithProfile(ITypedElement element, string profileUri)
        {
            return _validator.Validate(element, profileUri);
        }
    }
}
