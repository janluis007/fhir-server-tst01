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
    public static class Host
    {
        private static readonly InProcTestFhirServer Server;
        private static readonly TestFhirClient FhirClient;

#pragma warning disable CA1810 // Initialize reference type static fields inline
        static Host()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            var functionPath = Path.Combine(new FileInfo(typeof(Host).Assembly.Location).Directory.FullName, "..");
            Environment.SetEnvironmentVariable("HOST_FUNCTION_CONTENT_PATH", functionPath, EnvironmentVariableTarget.Process);

            Server = new InProcTestFhirServer(Tests.Common.FixtureParameters.DataStore.CosmosDb, typeof(Startup));
            FhirClient = Server.GetTestFhirClient(Hl7.Fhir.Rest.ResourceFormat.Json, true);
        }

        /// <summary>
        /// This trigger covers all routes, except those reserved by Azure Functions and will keep the portal running as expected.
        /// </summary>
        /// <param name="req">The http request</param>
        /// <param name="log">The logger</param>
        /// <param name="ctx">Execution context</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>HttpResponse built by the WebHost.</returns>
        [FunctionName("AllPaths")]
        public static async Task<HttpResponseMessage> RunAllPaths(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "patch", "options", Route = "{*x:regex(^(?!admin|debug|runtime).*$)}")]HttpRequestMessage req,
            ILogger log,
            System.Threading.ExecutionContext ctx,
            CancellationToken ct)
        {
            return await FhirClient.HttpClient.SendAsync(req);
        }

        /// <summary>
        /// This trigger covers root route only which isn't caught by the other HttpTrigger.
        /// In order to have this working, the AppSettings require the following settings:
        ///    "AzureWebJobsDisableHomepage": "true"
        /// </summary>
        /// <param name="req">The http request</param>
        /// <param name="log">The logger</param>
        /// <param name="ctx">Execution context</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>HttpResponse built by the WebHost.</returns>
        [FunctionName("Root")]
        public static async Task<HttpResponseMessage> RunRoot(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "patch", "options", Route = "/")]HttpRequestMessage req,
            ILogger log,
            System.Threading.ExecutionContext ctx,
            CancellationToken ct)
        {
            return await FhirClient.HttpClient.SendAsync(req, ct);
        }
    }
}
