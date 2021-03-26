// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Fhir.Core.Messages.Migrate
{
    public class MigrateRequest : IRequest<MigrateResponse>
    {
        public MigrateRequest(string migrateId, MigrateRequestType requestType, int migrationCount)
        {
            Id = migrateId;
            RequestType = requestType;
            MigrationCount = migrationCount;
        }

        public string Id { get; set; }

        public int MigrationCount { get; set; }

        public MigrateRequestType RequestType { get; set; }
    }
}
