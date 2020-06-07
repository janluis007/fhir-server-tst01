// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Hl7.Fhir.ElementModel;

namespace Microsoft.Health.Fhir.Core.Serialization.SourceNodes
{
    internal class MetaSourceNode : BaseSourceNode<MetaBase>
    {
        private readonly string _name;
        private readonly string _location;

        public MetaSourceNode(MetaBase resource, string name, string location)
            : base(resource)
        {
            _name = name;
            _location = location;
        }

        public override string Name => _name;

        public override string Text => null;

        public override string Location => _location;

        public override string ResourceType => null;

        protected override IEnumerable<(string Name, Lazy<IEnumerable<ISourceNode>> Node)> PropertySourceNodes()
        {
            yield return ("versionId", new Lazy<IEnumerable<ISourceNode>>(() => new[] { new FhirStringSourceNode(() => Resource.VersionId, "versionId", $"{_location}.versionId") }));
            yield return ("lastUpdated", new Lazy<IEnumerable<ISourceNode>>(() => new[] { new FhirStringSourceNode(() => Resource.LastUpdated, "lastUpdated", $"{_location}.lastUpdated") }));
        }
    }
}
