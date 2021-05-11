// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Core.Features.Operations.Everything
{
    public class EverythingOperationContinuationToken
    {
        public EverythingOperationContinuationToken(string phase, string internalContinuationToken)
        {
            Phase = phase;
            InternalContinuationToken = internalContinuationToken;
        }

        public string Phase { get; }

        public string InternalContinuationToken { get; }
    }
}
