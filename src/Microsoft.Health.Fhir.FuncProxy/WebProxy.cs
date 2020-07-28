// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Health.Fhir.Web;

namespace Microsoft.Health.Fhir.FuncProxy
{
    public static class WebProxy
    {
        [FunctionName("WebProxy")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "patch", "options", Route = "{*any}")] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var configRoot = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json")
                .Build();

            var hostingEnvironment = new MyHostingEnvironment()
            {
                ContentRootPath = context.FunctionAppDirectory,
                WebRootPath = context.FunctionAppDirectory,
            };

            hostingEnvironment.WebRootFileProvider = hostingEnvironment.ContentRootFileProvider;

            /* Add required services into DI container */
            var services = new ServiceCollection();

            // services.AddSingleton<DiagnosticSource>(new DiagnosticListener("Microsoft.AspNetCore"));
            // services.AddSingleton<ObjectPoolProvider>(new DefaultObjectPoolProvider());
            // services.AddSingleton<IHostEnvironment>(hostingEnvironment);
            services.AddSingleton<IWebHostEnvironment>(hostingEnvironment);

            // services.AddSingleton<IConfiguration>(configRoot);

            /* Instantiate standard ASP.NET Core Startup class */
            var startup = new Startup(configRoot);

            /* Add web app services into DI container */
            startup.ConfigureServices(services);

            /* Initialize DI container */
            var serviceProvider = services.BuildServiceProvider();

            /* Initialize Application builder */
            var appBuilder = new ApplicationBuilder(serviceProvider, new FeatureCollection());

            /* Configure the HTTP request pipeline */
            startup.Configure(appBuilder);

            /* Build request handling function */
            var requestHandler = appBuilder.Build();

            /* Set DI container for HTTP Context */
            req.HttpContext.RequestServices = serviceProvider;

            /* Handle HTTP request */
            await requestHandler(req.HttpContext);

            /* This dummy result does nothing, HTTP response is already set by requestHandler */
            return new EmptyResult();
        }
    }
}
