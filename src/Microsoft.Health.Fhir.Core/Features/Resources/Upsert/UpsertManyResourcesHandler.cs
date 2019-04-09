// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Conformance;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Messages.Upsert;

namespace Microsoft.Health.Fhir.Core.Features.Resources.Upsert
{
    public class UpsertManyResourcesHandler : BaseResourceHandler, IRequestHandler<UpsertManyResourcesRequest, UpsertManyResourcesResponse>
    {
        public UpsertManyResourcesHandler(
            IDataStore dataStore,
            Lazy<IConformanceProvider> conformanceProvider,
            IResourceWrapperFactory resourceWrapperFactory)
            : base(dataStore, conformanceProvider, resourceWrapperFactory)
        {
        }

        public async Task<UpsertManyResourcesResponse> Handle(UpsertManyResourcesRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            await DataStore.UpsertManyAsync(message.Resources, true, false, cancellationToken);

            return new UpsertManyResourcesResponse();
        }

        protected override void AddResourceCapability(ListedCapabilityStatement statement, ResourceType resourceType)
        {
            statement.TryAddRestInteraction(resourceType, CapabilityStatement.TypeRestfulInteraction.Update);
        }
    }
}
