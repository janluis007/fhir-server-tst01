// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.IO;
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
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Web;

namespace Microsoft.Health.Fhir.FuncProxy
{
    public static class WebProxy
    {
        private static ServiceCollection services;
        private static ServiceProvider serviceProvider;
        private static ApplicationBuilder appBuilder;
        private static Startup startup;
        private static RequestDelegate requestHandler;

#pragma warning disable CA1810 // Initialize reference type static fields inline
        static WebProxy()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            var functionPath = Path.Combine(new FileInfo(typeof(WebProxy).Assembly.Location).Directory.FullName, "..");

            var configRoot = new ConfigurationBuilder()
                .SetBasePath(functionPath)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json")
                .Build();

            IFileProvider fileProvider = new PhysicalFileProvider(functionPath);

            var hostingEnvironment = new MyHostingEnvironment()
            {
                ContentRootPath = functionPath,
                WebRootPath = functionPath,
                ContentRootFileProvider = fileProvider,
                WebRootFileProvider = fileProvider,
            };

            var conttents = hostingEnvironment.ContentRootFileProvider.GetDirectoryContents(".");

            hostingEnvironment.WebRootFileProvider = hostingEnvironment.ContentRootFileProvider;

            /* Add required services into DI container */
            services = new ServiceCollection();
            var listener = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton<DiagnosticSource>(listener);

            // services.AddSingleton<ObjectPoolProvider>(new DefaultObjectPoolProvider());
            // services.AddSingleton<IHostEnvironment>(hostingEnvironment);
            services.AddSingleton<IWebHostEnvironment>(hostingEnvironment);
            services.AddSingleton<IConfiguration>(configRoot);
            services.AddSingleton(listener);

            // services.AddSingleton<IConfiguration>(configRoot);

            /* Instantiate standard ASP.NET Core Startup class */
            startup = new Startup(configRoot);

            /* Add web app services into DI container */
            startup.ConfigureServices(services);

            /* Initialize DI container */
            serviceProvider = services.BuildServiceProvider();

            /* Initialize Application builder */
            appBuilder = new ApplicationBuilder(serviceProvider, new FeatureCollection());

            /* Configure the HTTP request pipeline */
            startup.Configure(appBuilder);

            /* Build request handling function */
            requestHandler = appBuilder.Build();

            foreach (var startable in serviceProvider.GetServices<IStartable>())
            {
                startable.Start();
            }

            foreach (var initializable in serviceProvider.GetServices<IRequireInitializationOnFirstRequest>())
            {
                initializable.EnsureInitialized().GetAwaiter().GetResult();
            }
        }

        [FunctionName("WebProxy")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "patch", "options", Route = "{*any}")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            /* Set DI container for HTTP Context */
            req.HttpContext.RequestServices = serviceProvider;

            /* Handle HTTP request */
            await requestHandler(req.HttpContext);

            /* This dummy result does nothing, HTTP response is already set by requestHandler */
            return new EmptyResult();
        }
    }
}
