// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Serialization;
using Hl7.FhirPath;
using Microsoft.Health.Fhir.Core.Features.Serialization;
using Microsoft.Health.Fhir.Core.Features.Serialization.SourceNodes.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Models
{
    /// <summary>
    /// Wraps an ITypedElement that contains generic FHIR data
    /// </summary>
    public class ResourceElement
    {
        private readonly FhirJsonTextNode _sourceNode;
        private readonly bool _isReadOnly = false;
        private readonly Lazy<EvaluationContext> _context = new Lazy<EvaluationContext>(() => new EvaluationContext());
        private readonly Lazy<ITypedElement> _typedElement;
        private readonly IResourceElementPropertyAccessor _propertyAccessor;

        private readonly List<string> _nonDomainTypes = new List<string>
        {
            "Bundle",
            "Parameters",
            "Binary",
        };

        public ResourceElement(ITypedElement sourceNode)
        {
            EnsureArg.IsNotNull(sourceNode, nameof(sourceNode));

            _typedElement = new Lazy<ITypedElement>(() => sourceNode);
            _propertyAccessor = new FhirPathPropertyAccessor(sourceNode, _context);
            _isReadOnly = true;
        }

        public ResourceElement(ISourceNode sourceNode, IModelInfoProvider modelInfoProvider)
        {
            EnsureArg.IsNotNull(sourceNode, nameof(sourceNode));
            EnsureArg.IsNotNull(modelInfoProvider, nameof(modelInfoProvider));

            if (sourceNode is FhirJsonTextNode node)
            {
                _sourceNode = node;
            }
            else
            {
                _sourceNode = (FhirJsonTextNode)JsonSourceNodeFactory.Parse(sourceNode.ToTypedElement(modelInfoProvider.StructureDefinitionSummaryProvider).ToJson());
            }

            _propertyAccessor = new ResourceJsonNodePropertyAccessor(_sourceNode.Resource);
            _typedElement = new Lazy<ITypedElement>(() => _sourceNode.ToTypedElement(modelInfoProvider.StructureDefinitionSummaryProvider));
        }

        public string InstanceType => _propertyAccessor.InstanceType;

        public ITypedElement Instance => _typedElement.Value;

        public ResourceJsonNode Resource => _sourceNode?.Resource;

        public string Id => _propertyAccessor.Id;

        public string VersionId => _propertyAccessor.VersionId;

        public bool IsDomainResource => !_nonDomainTypes.Contains(InstanceType, StringComparer.OrdinalIgnoreCase);

        public DateTimeOffset? LastUpdated
        {
            get
            {
                var obj = _propertyAccessor.LastUpdated;
                if (obj != null)
                {
                    return PrimitiveTypeConverter.ConvertTo<DateTimeOffset>(obj);
                }

                return null;
            }
        }

        public T Scalar<T>(string fhirPath)
        {
            object scalar = Instance.Scalar(fhirPath, _context.Value);
            return (T)scalar;
        }

        public IEnumerable<ITypedElement> Select(string fhirPath)
        {
            return Instance.Select(fhirPath, _context.Value);
        }

        public bool Predicate(string fhirPath)
        {
            return Instance.Predicate(fhirPath, _context.Value);
        }

        public ResourceElement UpdateId(string id)
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException();
            }

            _sourceNode.Resource.Id = id;

            return this;
        }

        public ResourceElement UpdateVersion(string version)
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException();
            }

            _sourceNode.Resource.Meta.VersionId = version;

            return this;
        }

        public ResourceElement UpdateLastUpdated(DateTimeOffset? lastUpdated)
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException();
            }

            _sourceNode.Resource.Meta.VersionId = lastUpdated?.ToString("o", CultureInfo.InvariantCulture);

            return this;
        }

        public string ToJson(FhirJsonSerializationSettings settings = null)
        {
            if (_sourceNode != null)
            {
                return _sourceNode.SerializeToJson(settings?.Pretty ?? false);
            }
            else
            {
                return Instance.ToJson(settings);
            }
        }

        public void WriteTo(JsonWriter writer, FhirJsonSerializationSettings settings = null)
        {
            if (_sourceNode != null)
            {
                // _sourceNode.WriteTo(writer, settings);
                writer.WriteRaw(ToJson(settings));
            }
            else
            {
                Instance.WriteTo(writer, settings);
            }
        }

        private class FhirPathPropertyAccessor : IResourceElementPropertyAccessor
        {
            private readonly ITypedElement _typedElement;
            private readonly Lazy<EvaluationContext> _evaluationContext;

            public FhirPathPropertyAccessor(ITypedElement typedElement, Lazy<EvaluationContext> evaluationContext)
            {
                EnsureArg.IsNotNull(typedElement, nameof(typedElement));

                _typedElement = typedElement;
                _evaluationContext = evaluationContext;
            }

            public string Id => (string)_typedElement.Scalar("Resource.id", _evaluationContext.Value);

            public string LastUpdated => _typedElement.Scalar("Resource.meta.lastUpdated", _evaluationContext.Value)?.ToString();

            public string VersionId => (string)_typedElement.Scalar("Resource.meta.versionId", _evaluationContext.Value);

            public string InstanceType => _typedElement.InstanceType;
        }

        private class ResourceJsonNodePropertyAccessor : IResourceElementPropertyAccessor
        {
            private readonly ResourceJsonNode _element;

            public ResourceJsonNodePropertyAccessor(ResourceJsonNode element)
            {
                EnsureArg.IsNotNull(element, nameof(element));

                _element = element;
            }

            public string Id => _element.Id;

            public string LastUpdated => _element.Meta?.LastUpdated;

            public string VersionId => _element.Meta?.VersionId;

            public string InstanceType => _element.ResourceType;
        }
    }
}
