﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.Tests.Common.FixtureParameters;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Fhir.Tests.E2E.Rest.Sql
{
    [HttpIntegrationFixtureArgumentSets(DataStore.SqlServer, Format.Json)]
    public class SchemaTests : IClassFixture<HttpIntegrationTestFixture>
    {
        private readonly HttpClient _client;

        public SchemaTests(HttpIntegrationTestFixture fixture)
        {
            _client = fixture.HttpClient;
        }

        public static IEnumerable<object[]> Data =>
            new List<object[]>
            {
                new object[] { "_schema/compatibility" },
                new object[] { "_schema/versions/current" },
            };

        [Fact]
        public async Task WhenRequestingAvailable_GivenAServerThatHasSchemas_JsonShouldBeReturned()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_client.BaseAddress, "_schema/versions"),
            };

            HttpResponseMessage response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var jArrayResponse = JArray.Parse(await response.Content.ReadAsStringAsync());

            Assert.NotEmpty(jArrayResponse);

            JToken firstResult = jArrayResponse.First;
            string scriptUrl = $"{_client.BaseAddress}_schema/versions/{firstResult["id"]}/script";
            Assert.Equal(scriptUrl, firstResult["script"]);
        }

        [Fact]
        public async Task WhenRequestingSchema_GivenGetMethodAndCompatibilityPathAndInstanceSchemaTableIsEmpty_TheServerShouldReturnsNotFound()
        {
            // Since Instance Schema information table is empty
            HttpResponseMessage response = await SendAndVerifyStatusCode(HttpMethod.Get, "_schema/compatibility", HttpStatusCode.NotFound);

            string responseBodyAsText = await response.Content.ReadAsStringAsync();
            Assert.Contains("The compatibility information is not found.", responseBodyAsText);
        }

        [Fact]
        public async Task WhenRequestingSchema_GivenGetMethodAndCurrentVersionPath_TheServerShouldReturnNotImplemented()
        {
            await SendAndVerifyStatusCode(HttpMethod.Get, "_schema/versions/current", HttpStatusCode.NotImplemented);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public async Task WhenRequestingSchema_GivenPostMethod_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Post, path, HttpStatusCode.NotFound);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public async Task WhenRequestingSchema_GivenPutMethod_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Put, path, HttpStatusCode.NotFound);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public async Task WhenRequestingSchema_GivenDeleteMethod_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Delete, path, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task WhenRequestingScript_GivenNonIntegerVersion_TheServerShouldReturnNotFound()
        {
            await SendAndVerifyStatusCode(HttpMethod.Get, "_schema/versions/abc/script", HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task WhenRequestingScript_GivenPostMethod_TheServerShouldReturnNotFound()
        {
            await SendAndVerifyStatusCode(HttpMethod.Post, "_schema/versions/1/script", HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task WhenRequestingScript_GivenPutMethod_TheServerShouldReturnNotFound()
        {
            await SendAndVerifyStatusCode(HttpMethod.Put, "_schema/versions/1/script", HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task WhenRequestingScript_GivenDeleteMethod_TheServerShouldReturnNotFound()
        {
            await SendAndVerifyStatusCode(HttpMethod.Delete, "_schema/versions/1/script", HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task WhenRequestingScript_GivenSchemaIdFound_TheServerShouldReturnScript()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_client.BaseAddress, "_schema/versions/1/script"),
            };
            HttpResponseMessage response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string script = response.Content.ToString();

            Assert.NotEmpty(script);
        }

        [Fact]
        public async Task WhenRequestingScript_GivenSchemaIdNotFound_TheServerShouldReturnNotFoundException()
        {
            await SendAndVerifyStatusCode(HttpMethod.Get, "_schema/versions/0/script", HttpStatusCode.NotFound);
        }

        private async Task<HttpResponseMessage> SendAndVerifyStatusCode(HttpMethod httpMethod, string path, HttpStatusCode httpStatusCode)
        {
            var request = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = new Uri(_client.BaseAddress, path),
            };
            HttpResponseMessage response = null;

            // Setting the contentType explicitly because POST/PUT/PATCH throws UnsupportedMediaType
            using (var content = new StringContent(" ", Encoding.UTF8, "application/json"))
            {
                request.Content = content;
                response = await _client.SendAsync(request);
                Assert.Equal(httpStatusCode, response.StatusCode);
            }

            return response;
        }
    }
}
