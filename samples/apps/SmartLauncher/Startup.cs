// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Internal.SmartLauncher.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Internal.SmartLauncher
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
#pragma warning disable CA1801 // Review unused parameters
        public void ConfigureServices(IServiceCollection services)
#pragma warning restore CA1801 // Review unused parameters
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions { FileProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly(), "Microsoft.Health.Internal.SmartLauncher.wwwroot") });

            app.Map("/config", a =>
            {
                a.Run(async (context) =>
                {
                    var config = new SmartLauncherConfig();
                    Configuration.Bind(config);
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(config));
                });
            });
        }
    }
}
