// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Messages.Upsert;
using Microsoft.Health.Fhir.Core.Registration;
using Task = System.Threading.Tasks.Task;

namespace SandboxImporter
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            EnsureArg.IsNotNull(args, nameof(args));

            await Task.CompletedTask;

            var host = WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.SetBasePath(Directory.GetCurrentDirectory());
                    configApp.AddJsonFile("importerappsettings.json", optional: false);
                    configApp.AddJsonFile(
                        $"importerappsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                        optional: true);
                    configApp.AddEnvironmentVariables(prefix: "PREFIX_");
                })
                .ConfigureServices((context, collection) =>
                {
                    IFhirServerBuilder fhirServerBuilder = collection.AddFhirServer(context.Configuration);

                    fhirServerBuilder.Services.Add(provider =>
                        {
                            var config = new SqlServerDataStoreConfiguration();
                            provider.GetService<IConfiguration>().GetSection("SqlServer").Bind(config);

                            return config;
                        })
                        .Singleton()
                        .AsSelf();

                    fhirServerBuilder.Services.Add<SqlServerDataStore>()
                        .Singleton()
                        .AsSelf()
                        .AsImplementedInterfaces();

                    collection.Add<UrlHelperFactory>()
                        .Singleton()
                        .ReplaceService<IUrlHelperFactory>();
                })
                .Configure(builder => { })
                .Build();

            foreach (var startable in host.Services.GetRequiredService<IEnumerable<IStartable>>())
            {
                startable.Start();
            }

            using (var serviceScope = host.Services.CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext();
                serviceScope.ServiceProvider.GetRequiredService<IFhirRequestContextAccessor>().FhirRequestContext = new FhirRequestContext("PUT", "https://x", "https://y", new Coding("a", "b"), "w", ImmutableDictionary<string, StringValues>.Empty, ImmutableDictionary<string, StringValues>.Empty);
                var resourceWrapperFactory = serviceScope.ServiceProvider.GetRequiredService<IResourceWrapperFactory>();

                var mediator = serviceScope.ServiceProvider.GetRequiredService<IMediator>();

                var fhirJsonParser = new FhirJsonParser();
                var overallSw = Stopwatch.StartNew();

                int patientCount = 0;

                var batchTracker = new BatchTracker();

                var parseNdJson = new TransformBlock<string, UpsertManyResourcesRequest>(
                    async ndJsonPath =>
                    {
                        string[] resourceLines = await File.ReadAllLinesAsync(ndJsonPath);
                        var bundleTracker = new BundleTracker(batchTracker, resourceLines.Length);
                        return new UpsertManyResourcesRequest(resourceLines.Select(l => resourceWrapperFactory.Create(fhirJsonParser.Parse<Resource>(l), false)).ToList());
                    },
                    new ExecutionDataflowBlockOptions { BoundedCapacity = 100, MaxDegreeOfParallelism = Environment.ProcessorCount });

                var upsertBundle = new ActionBlock<UpsertManyResourcesRequest[]>(
                    async r =>
                    {
                        try
                        {
                            await mediator.Send<UpsertManyResourcesResponse>(new UpsertManyResourcesRequest(r.SelectMany(r2 => r2.Resources).ToList()));
                            batchTracker.CompleteBundles(r.Length);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    },
                    new ExecutionDataflowBlockOptions { BoundedCapacity = 100, MaxDegreeOfParallelism = Environment.ProcessorCount * 2 });

                var batchBlock = new BatchBlock<UpsertManyResourcesRequest>(1);
                parseNdJson.LinkTo(batchBlock, new DataflowLinkOptions() { PropagateCompletion = true });
                batchBlock.LinkTo(upsertBundle, new DataflowLinkOptions() { PropagateCompletion = true });

                string ndJsonDir = serviceScope.ServiceProvider.GetRequiredService<IConfiguration>().GetValue<string>("ndJsonPath");

                batchTracker.Start();

                foreach (var ndJson in Directory.EnumerateFiles(ndJsonDir))
                {
                    patientCount++;
                    await parseNdJson.SendAsync(ndJson);
                }

                parseNdJson.Complete();
                await upsertBundle.Completion;

                Console.WriteLine($"All done. {patientCount} patients uploaded in {overallSw.Elapsed}. {(int)(patientCount / overallSw.Elapsed.TotalHours)} patients per hour");
                Console.ReadKey();
            }
        }

        private class BatchTracker : IDisposable
        {
            private volatile int _count;
            private Stopwatch _sw;
            private TimeSpan _lastTime;
            private int _lastCount;
            private Timer _timer;
            private TimeSpan _period;

            public void CompleteBundles(int batchSize)
            {
                int countSnapshot = Interlocked.Add(ref _count, batchSize);
            }

            public void Start()
            {
                _sw = Stopwatch.StartNew();
                _period = TimeSpan.FromSeconds(20);
                _timer = new Timer(
                    state =>
                    {
                        var currentTime = _sw.Elapsed;
                        var currentCount = _count;
                        Console.WriteLine($"{currentCount} bundles uploaded. Current rate is {(int)((currentCount - _lastCount) / (currentTime - _lastTime).TotalHours)} bundles/hour. Elapsed time: {currentTime.ToString(@"hh\:mm\:ss")}. Overall average: {(int)(currentCount / currentTime.TotalHours)} bundles/hour.");
                        _lastCount = _count;
                        _lastTime = currentTime;
                    },
                    null,
                    _period,
                    _period);
            }

            public void Dispose()
            {
                _timer?.Dispose();
            }
        }

        private class BundleTracker
        {
            private BatchTracker _batchTracker;
            private int _countRemaining;

            public BundleTracker(BatchTracker batchTracker, int count)
            {
                _batchTracker = batchTracker;
                _countRemaining = count;
            }

            public void CompleteOne()
            {
                if (Interlocked.Decrement(ref _countRemaining) == 0)
                {
                    _batchTracker.CompleteBundles(1);
                }
            }
        }
    }
}
