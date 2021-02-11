// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using MediatR;

namespace Microsoft.Health.Fhir.Core.Messages.Operation
{
    public class GetSortSettingsRequest : IRequest<SortSettingsResponse>
    {
        public GetSortSettingsRequest(Uri searchParameterUri)
        {
            EnsureArg.IsNotNull(searchParameterUri, nameof(searchParameterUri));

            SearchParameterUri = searchParameterUri;
        }

        public Uri SearchParameterUri { get; }
    }
}
