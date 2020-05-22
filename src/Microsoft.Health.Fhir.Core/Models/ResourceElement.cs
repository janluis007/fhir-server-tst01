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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Core.Models
{
    /// <summary>
    /// Wraps an ITypedElement that contains generic FHIR data
    /// </summary>
    public class ResourceElement
    {
        private readonly FhirJsonNode _sourceNode;
        private readonly IModelInfoProvider _modelInfoProvider;
        private readonly bool isReadOnly = false;
        private readonly Lazy<EvaluationContext> _context = new Lazy<EvaluationContext>(() => new EvaluationContext());

        private readonly List<string> _nonDomainTypes = new List<string>
        {
            "Bundle",
            "Parameters",
            "Binary",
        };

        private Lazy<ITypedElement> _typedElement;
        private readonly JsonMergeSettings _jsonMergeSettings = new JsonMergeSettings
        {
            PropertyNameComparison = StringComparison.OrdinalIgnoreCase,
            MergeNullValueHandling = MergeNullValueHandling.Merge,
            MergeArrayHandling = MergeArrayHandling.Union,
        };

        public ResourceElement(ITypedElement sourceNode)
        {
            EnsureArg.IsNotNull(sourceNode, nameof(sourceNode));

            _typedElement = new Lazy<ITypedElement>(() => sourceNode);
            isReadOnly = true;
        }

        public ResourceElement(ISourceNode sourceNode, IModelInfoProvider modelInfoProvider)
        {
            EnsureArg.IsNotNull(sourceNode, nameof(sourceNode));
            EnsureArg.IsNotNull(modelInfoProvider, nameof(modelInfoProvider));

            if (sourceNode is FhirJsonNode node)
            {
                _sourceNode = node;
            }
            else
            {
                _sourceNode = (FhirJsonNode)FhirJsonNode.Parse(sourceNode.ToTypedElement(modelInfoProvider.StructureDefinitionSummaryProvider).ToJson());
            }

            _modelInfoProvider = modelInfoProvider;
            SetupTypedElement();
        }

        public string InstanceType => _sourceNode != null ? _sourceNode.GetResourceTypeIndicator() : _typedElement.Value.InstanceType;

        public ITypedElement Instance => _typedElement.Value;

        public string Id => Scalar<string>("Resource.id");

        public string VersionId => Scalar<string>("Resource.meta.versionId");

        public bool IsDomainResource => !_nonDomainTypes.Contains(InstanceType, StringComparer.OrdinalIgnoreCase);

        public DateTimeOffset? LastUpdated
        {
            get
            {
                var obj = Instance.Scalar("Resource.meta.lastUpdated");
                if (obj != null)
                {
                    return PrimitiveTypeConverter.ConvertTo<DateTimeOffset>(obj.ToString());
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
            if (isReadOnly)
            {
                throw new NotSupportedException();
            }

            _sourceNode.JsonObject.Merge(JObject.FromObject(new { id }), _jsonMergeSettings);

            return this;
        }

        public ResourceElement UpdateVersion(string version)
        {
            if (isReadOnly)
            {
                throw new NotSupportedException();
            }

            _sourceNode.JsonObject.Merge(JObject.FromObject(new { meta = new { versionId = version } }), _jsonMergeSettings);

            return this;
        }

        public ResourceElement UpdateLastUpdated(DateTimeOffset? lastUpdated)
        {
            if (isReadOnly)
            {
                throw new NotSupportedException();
            }

            _sourceNode.JsonObject.Merge(JObject.FromObject(new { meta = new { lastUpdated = lastUpdated?.ToString("o", CultureInfo.InvariantCulture) } }), _jsonMergeSettings);

            return this;
        }

        public string ToJson(FhirJsonSerializationSettings settings = null)
        {
            if (_sourceNode != null)
            {
                return _sourceNode.ToJson(settings);
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
                _sourceNode.WriteTo(writer, settings);
            }
            else
            {
                Instance.WriteTo(writer, settings);
            }
        }

        private void SetupTypedElement()
        {
            if (isReadOnly)
            {
                throw new NotSupportedException();
            }

            _typedElement = new Lazy<ITypedElement>(() => _sourceNode.ToTypedElement(_modelInfoProvider.StructureDefinitionSummaryProvider));
        }
    }
}
