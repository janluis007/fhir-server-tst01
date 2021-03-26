// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Fhir.Core.Features.Persistence;

namespace Microsoft.Health.Fhir.Core.Models
{
    public class RawResourceElement : IResourceElement
    {
        public RawResourceElement(ResourceWrapper wrapper)
            : this(EnsureArg.IsNotNull(wrapper, nameof(wrapper)).RawResource, wrapper.ResourceId, wrapper.Version, wrapper.ResourceTypeName, wrapper.LastModified)
        {
        }

        public RawResourceElement(RawResource rawResource, string id, string versionId, string instanceType, DateTimeOffset? lastUpdated)
        {
            EnsureArg.IsNotNull(rawResource, nameof(rawResource));

            RawResource = rawResource;
            Format = rawResource.Format;
            Id = id;
            VersionId = versionId;
            InstanceType = instanceType;
            LastUpdated = lastUpdated;
        }

        public RawResource RawResource { get; protected set; }

        public FhirResourceFormat Format { get; protected set; }

        public string Id { get; protected set; }

        public string VersionId { get; protected set; }

        public string InstanceType { get; protected set; }

        public DateTimeOffset? LastUpdated { get; protected set; }
    }
}
