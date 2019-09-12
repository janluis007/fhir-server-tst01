// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;

namespace Microsoft.Health.Fhir.Core.Features.Resources
{
    public class ResourceModifierEngine
    {
        private readonly Dictionary<Type, List<IResourceModifier>> _resourceModifiers;

        public ResourceModifierEngine(IEnumerable<IResourceModifier> resourceModifiers)
        {
            _resourceModifiers = new Dictionary<Type, List<IResourceModifier>>();
            foreach (var modifier in resourceModifiers)
            {
                if (_resourceModifiers.ContainsKey(modifier.TargetType))
                {
                    _resourceModifiers[modifier.TargetType].Add(modifier);
                }
                else
                {
                    _resourceModifiers[modifier.TargetType] = new List<IResourceModifier>
                    {
                        modifier,
                    };
                }
            }
        }

        public void Modify(Resource resource)
        {
            if (_resourceModifiers.ContainsKey(resource.GetType()))
            {
                foreach (var modifier in _resourceModifiers[resource.GetType()])
                {
                    modifier.Modify(resource);
                }
            }
        }
    }
}
