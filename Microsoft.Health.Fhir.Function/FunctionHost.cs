// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Tests.E2E.Common;
using Microsoft.Health.Fhir.Tests.E2E.Rest;
using Microsoft.Health.Fhir.Web;

namespace Microsoft.Health.Fhir.Function
{
    public static class FunctionHost
    {
        private static readonly InProcTestFhirServer Server;

#pragma warning disable CA1810 // Initialize reference type static fields inline
        static FunctionHost()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            var functionPath = Path.Combine(new FileInfo(typeof(FunctionHost).Assembly.Location).Directory.FullName, "..");
            Environment.SetEnvironmentVariable("HOST_FUNCTION_CONTENT_PATH", functionPath, EnvironmentVariableTarget.Process);

            Server = new InProcTestFhirServer(Tests.Common.FixtureParameters.DataStore.CosmosDb, typeof(Startup), functionPath);
        }

        /// <summary>
        /// This trigger covers all routes, except those reserved by Azure Functions and will keep the portal running as expected.
        /// </summary>
        /// <param name="req">The http request</param>
        /// <param name="log">The logger</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>HttpResponse built by the WebHost.</returns>
        [FunctionName("AllPaths")]
        public static async Task<HttpResponseMessage> RunAllPaths(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "patch", "options", Route = "{*any}")]HttpRequestMessage req,
            ILogger log,
            CancellationToken ct)
        {
            log.LogInformation(req.RequestUri.ToString());
            var fhirClient = Server.GetTestFhirClient(Hl7.Fhir.Rest.ResourceFormat.Json, true);
            return await fhirClient.HttpClient.SendAsync(req, ct);
        }

        /*
        /// <summary>
        /// This trigger covers root route only which isn't caught by the other HttpTrigger.
        /// In order to have this working, the AppSettings require the following settings:
        ///    "AzureWebJobsDisableHomepage": "true"
        /// </summary>
        /// <param name="req">The http request</param>
        /// <param name="log">The logger</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>HttpResponse built by the WebHost.</returns>
        [FunctionName("Root")]
        public static async Task<HttpResponseMessage> RunRoot(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "patch", "options", Route = "/")]HttpRequestMessage req,
            ILogger log,
            CancellationToken ct)
        {
            log.LogInformation(req.RequestUri.ToString());
            return await FhirClient.HttpClient.SendAsync(req, ct);
        } */
    }
}
