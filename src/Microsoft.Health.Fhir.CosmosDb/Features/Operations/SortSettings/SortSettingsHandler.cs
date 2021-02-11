// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Exceptions;
using Microsoft.Health.Fhir.Core.Features.Definition;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Search.Registry;
using Microsoft.Health.Fhir.Core.Features.Security;
using Microsoft.Health.Fhir.Core.Features.Security.Authorization;
using Microsoft.Health.Fhir.Core.Messages.Operation;
using Microsoft.Health.Fhir.Core.Messages.Search;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.ValueSets;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Operations.SortSettings
{
    public class SortSettingsHandler : IRequestHandler<UpdateSortSettingsRequest, SortSettingsResponse>, IRequestHandler<GetSortSettingsRequest, SortSettingsResponse>
    {
        private readonly ISearchParameterStatusDataStore _statusManager;
        private readonly ISearchParameterDefinitionManager _searchParameterDefinitionManager;
        private readonly IFhirAuthorizationService _authorizationService;
        private readonly IMediator _mediator;

        public SortSettingsHandler(ISearchParameterStatusDataStore statusManager, ISearchParameterDefinitionManager searchParameterDefinitionManager, IFhirAuthorizationService authorizationService, IMediator mediator)
        {
            EnsureArg.IsNotNull(statusManager, nameof(statusManager));
            EnsureArg.IsNotNull(searchParameterDefinitionManager, nameof(searchParameterDefinitionManager));
            EnsureArg.IsNotNull(authorizationService, nameof(authorizationService));
            EnsureArg.IsNotNull(mediator, nameof(mediator));

            _statusManager = statusManager;
            _searchParameterDefinitionManager = searchParameterDefinitionManager;
            _authorizationService = authorizationService;
            _mediator = mediator;
        }

        public async Task<SortSettingsResponse> Handle(UpdateSortSettingsRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await _authorizationService.CheckAccess(DataActions.Reindex) != DataActions.Reindex)
            {
                throw new UnauthorizedFhirActionException();
            }

            var uriString = request.ResourceElement.Scalar<string>(KnownSortParametersFhirPaths.UriValue);
            var setting = request.ResourceElement.Scalar<string>(KnownSortParametersFhirPaths.EnabledValue);

            var fhirUri = new Uri(uriString, UriKind.Absolute);

            ResourceSearchParameterStatus status = await _statusManager.GetSearchParameterStatus(fhirUri, cancellationToken);

            if (status == null)
            {
                throw new ResourceNotFoundException(string.Format(Core.Resources.SearchParameterDefinitionNotFound, fhirUri));
            }

            SearchParameterInfo info = _searchParameterDefinitionManager.GetSearchParameter(status.Uri);

            switch (info.Type)
            {
                case SearchParamType.String:
                case SearchParamType.Date:
                    break;
                default:
                    throw new BadRequestException(string.Format(CultureInfo.InvariantCulture, Core.Resources.SearchSortParameterNotSupported, info.Code));
            }

            if ((string.Equals(SortParameterStatus.Enabled.ToString(), setting, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(SortParameterStatus.Supported.ToString(), setting, StringComparison.OrdinalIgnoreCase))
                && status.SortStatus == SortParameterStatus.Disabled)
            {
                // Upgrade status from Disabled to Supported.
                status.SortStatus = SortParameterStatus.Supported;
                info.SortStatus = SortParameterStatus.Supported;

                await _statusManager.UpsertStatuses(new List<ResourceSearchParameterStatus> { status });
                await _mediator.Publish(new SearchParametersUpdated(new[] { info }));
            }
            else if (string.Equals(SortParameterStatus.Disabled.ToString(), setting, StringComparison.OrdinalIgnoreCase) && status.SortStatus != SortParameterStatus.Disabled)
            {
                // Request to Disable
                status.SortStatus = SortParameterStatus.Disabled;
                info.SortStatus = SortParameterStatus.Disabled;

                await _statusManager.UpsertStatuses(new List<ResourceSearchParameterStatus> { status });
                await _mediator.Publish(new SearchParametersUpdated(new[] { info }));
            }

            return new SortSettingsResponse(fhirUri, status.SortStatus);
        }

        public async Task<SortSettingsResponse> Handle(GetSortSettingsRequest request, CancellationToken cancellationToken)
        {
            if (await _authorizationService.CheckAccess(DataActions.Reindex) != DataActions.Reindex)
            {
                throw new UnauthorizedFhirActionException();
            }

            ResourceSearchParameterStatus status = await _statusManager.GetSearchParameterStatus(request.SearchParameterUri, cancellationToken);

            if (status == null)
            {
                throw new ResourceNotFoundException(string.Format(Core.Resources.SearchParameterDefinitionNotFound, request.SearchParameterUri));
            }

            return new SortSettingsResponse(request.SearchParameterUri, status.SortStatus);
        }
    }
}
