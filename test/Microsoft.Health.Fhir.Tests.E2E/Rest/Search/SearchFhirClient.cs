// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Health.Fhir.Tests.E2E.Common;
using FhirClient = Microsoft.Health.Fhir.Tests.E2E.Common.FhirClient;

namespace Microsoft.Health.Fhir.Tests.E2E.Rest.Search
{
    public class SearchFhirClient : FhirClient
    {
        private const string IdentifierSearchParameterName = "identifier";

        public SearchFhirClient(HttpClient httpClient, ResourceFormat format, string testSessionId)
            : base(httpClient, format)
        {
            TestSessionId = testSessionId;
        }

        private string TestSessionId { get; }

        private string IdentifierQueryString => $"{IdentifierSearchParameterName}={TestSessionId}";

        public override Task<FhirResponse<T>> CreateAsync<T>(string uri, T resource)
        {
            GetIdentifier(resource).Add(new Identifier(null, TestSessionId));

            return base.CreateAsync(uri, resource);
        }

        public override Task<FhirResponse<Bundle>> SearchAsync(ResourceType resourceType, string query = null, int? count = null)
        {
            query = query == null ?
                IdentifierQueryString :
                $"{IdentifierQueryString}&{query}";

            return base.SearchAsync(resourceType, query, count);
        }

        public override Task<FhirResponse<Bundle>> SearchAsync(string url)
        {
            if (!url.Contains(IdentifierQueryString))
            {
                char separator = url.Contains("?") ? '&' : '?';

                url = $"{url}{separator}{IdentifierQueryString}";
            }

            return base.SearchAsync(url);
        }

        private List<Identifier> GetIdentifier<T>(T resource)
            where T : Resource
        {
            switch (resource)
            {
                case Immunization immunization:
                    return immunization.Identifier;

                case Observation observation:
                    return observation.Identifier;

                case Organization organization:
                    return organization.Identifier;

                case Patient patient:
                    return patient.Identifier;

                case ValueSet valueSet:
                    return valueSet.Identifier;

                default:
                    throw new Exception($"The resource type {resource.ResourceType} is not supported.");
            }
        }
    }
}
