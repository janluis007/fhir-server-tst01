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
using Microsoft.Health.Fhir.Core.Serialization;
using Microsoft.Health.Fhir.Core.Serialization.SourceNodes;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Models
{
    /// <summary>
    /// Wraps an ITypedElement that contains generic FHIR data
    /// </summary>
    public class ResourceElement
    {
        private readonly FhirJsonTextNode2 _sourceNode;
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

            if (sourceNode is FhirJsonTextNode2 node)
            {
                _sourceNode = node;
            }
            else
            {
                _sourceNode = (FhirJsonTextNode2)FhirJsonTextNode2.Parse(sourceNode.ToTypedElement(modelInfoProvider.StructureDefinitionSummaryProvider).ToJson());
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

            // _sourceNode.JsonObject.Merge(JObject.FromObject(new { id }), _jsonMergeSettings);

            // _sourceNode.Merge(new { id });

            _sourceNode.Resource.Id = id;

            return this;
        }

        public ResourceElement UpdateVersion(string version)
        {
            if (isReadOnly)
            {
                throw new NotSupportedException();
            }

            // _sourceNode.JsonObject.Merge(JObject.FromObject(new { meta = new { versionId = version } }), _jsonMergeSettings);

            // _sourceNode.Merge(new { meta = new { versionId = version } });

            _sourceNode.Resource.Meta.VersionId = version;

            return this;
        }

        public ResourceElement UpdateLastUpdated(DateTimeOffset? lastUpdated)
        {
            if (isReadOnly)
            {
                throw new NotSupportedException();
            }

            // _sourceNode.JsonObject.Merge(JObject.FromObject(new { meta = new { lastUpdated = lastUpdated?.ToString("o", CultureInfo.InvariantCulture) } }), _jsonMergeSettings);

            // _sourceNode.Merge(new { meta = new { lastUpdated = lastUpdated?.ToString("o", CultureInfo.InvariantCulture) } });

            _sourceNode.Resource.Meta.VersionId = lastUpdated?.ToString("o", CultureInfo.InvariantCulture);

            return this;
        }

        public string ToJson(FhirJsonSerializationSettings settings = null)
        {
            if (_sourceNode != null)
            {
                return _sourceNode.ToRawJson();
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
