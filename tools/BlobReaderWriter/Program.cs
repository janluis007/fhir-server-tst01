﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Health.Fhir.Store.Utils;

namespace BlobReaderWriter
{
    public static class Program
    {
        private static readonly string SourceConnectionString = ConfigurationManager.AppSettings["SourceConnectionString"];
        private static readonly string TargetConnectionString = ConfigurationManager.AppSettings["TargetConnectionString"];
        private static readonly string SourceContainerName = ConfigurationManager.AppSettings["SourceContainerName"];
        private static readonly string TargetContainerName = ConfigurationManager.AppSettings["TargetContainerName"];
        private static readonly int Threads = int.Parse(ConfigurationManager.AppSettings["Threads"]);
        private static readonly int ReportingPeriodSec = int.Parse(ConfigurationManager.AppSettings["ReportingPeriodSec"]);
        ////private static readonly int MaxRetries = int.Parse(ConfigurationManager.AppSettings["MaxRetries"]);
        private static readonly int LinesPerBlob = int.Parse(ConfigurationManager.AppSettings["LinesPerBlob"]);
        private static readonly int SourceBlobs = int.Parse(ConfigurationManager.AppSettings["SourceBlobs"]);
        private static readonly bool WritesEnabled = bool.Parse(ConfigurationManager.AppSettings["WritesEnabled"]);

        public static void Main()
        {
            var sourceContainer = GetContainer(SourceConnectionString, SourceContainerName);
            var targetContainer = GetContainer(TargetConnectionString, TargetContainerName);
            var gPrefix = $"BlobReaderWriter.Threads={Threads}.Source={SourceContainerName}{(WritesEnabled ? $".Target={TargetContainerName}" : string.Empty)}";
            Console.WriteLine($"{gPrefix}: Starting at {DateTime.UtcNow.ToString("s")}...");
            var blobs = WritesEnabled
                      ? sourceContainer.GetBlobs().Where(_ => _.Name.EndsWith(".ndjson", StringComparison.OrdinalIgnoreCase)).OrderBy(_ => _.Name).Take(SourceBlobs)
                      : sourceContainer.GetBlobs();
            if (WritesEnabled)
            {
                Console.WriteLine($"{gPrefix}: SourceBlobs={blobs.Count()} at {DateTime.UtcNow.ToString("s")}.");
            }

            var sw = Stopwatch.StartNew();
            var swReport = Stopwatch.StartNew();
            var totalLines = 0L;
            var sourceBlobs = 0L;
            var targetBlobs = 0L;
            BatchExtensions.ExecuteInParallelBatches(blobs, Threads, 1, (thread, blobInt) =>
            {
                var lines = 0L;
                var blobIndex = blobInt.Item1;
                var blob = blobInt.Item2.First();

                var batch = new List<string>();
                var batchIndex = 0;
                foreach (var line in GetLinesInBlob(sourceContainer, blob))
                {
                    lines++;
                    if (WritesEnabled)
                    {
                        batch.Add(line);
                        if (batch.Count == LinesPerBlob)
                        {
                            WriteBatchOfLines(targetContainer, batch, GetTargetBlobName(blob.Name, batchIndex));
                            Interlocked.Increment(ref targetBlobs);
                            batch = new List<string>();
                            batchIndex++;
                        }
                    }
                }

                if (batch.Count > 0)
                {
                    WriteBatchOfLines(targetContainer, batch, GetTargetBlobName(blob.Name, batchIndex));
                    Interlocked.Increment(ref targetBlobs);
                }

                Interlocked.Add(ref totalLines, lines);
                Interlocked.Increment(ref sourceBlobs);

                if (swReport.Elapsed.TotalSeconds > ReportingPeriodSec)
                {
                    lock (swReport)
                    {
                        if (swReport.Elapsed.TotalSeconds > ReportingPeriodSec)
                        {
                            Console.WriteLine($"{gPrefix}: SourceBlobs={sourceBlobs}{(WritesEnabled ? $" TargetBlobs={targetBlobs}" : string.Empty)} Lines={totalLines} secs={(int)sw.Elapsed.TotalSeconds} speed={(int)(totalLines / sw.Elapsed.TotalSeconds)} lines/sec");
                            swReport.Restart();
                        }
                    }
                }
            });
            Console.WriteLine($"{gPrefix}.Total: SourceBlobs={sourceBlobs}{(WritesEnabled ? $" TargetBlobs={targetBlobs}" : string.Empty)} Lines={totalLines} secs={(int)sw.Elapsed.TotalSeconds} speed={(int)(totalLines / sw.Elapsed.TotalSeconds)} lines/sec");
        }

        private static string GetTargetBlobName(string sourceBlobName, int batchIndex)
        {
            return $"{sourceBlobName.Substring(0, sourceBlobName.Length - 7)}-{batchIndex}.ndjson";
        }

        private static void WriteBatchOfLines(BlobContainerClient container, IList<string> batch, string blobName)
        {
        retry:
            try
            {
                using var stream = container.GetBlockBlobClient(blobName).OpenWrite(true);
                using var writer = new StreamWriter(stream);
                foreach (var line in batch)
                {
                    writer.WriteLine(line);
                }

                writer.Flush();
            }
            catch (Exception e)
            {
                if (e.ToString().Contains("ConditionNotMet", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(e);
                    goto retry;
                }

                throw;
            }
        }

        private static IEnumerable<string> GetLinesInBlob(BlobContainerClient container, BlobItem blob)
        {
            using var reader = new StreamReader(container.GetBlobClient(blob.Name).Download().Value.Content);
            while (!reader.EndOfStream)
            {
                yield return reader.ReadLine();
            }
        }

        private static BlobContainerClient GetContainer(string connectionString, string containerName)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

                if (!blobContainerClient.Exists())
                {
                    var container = blobServiceClient.CreateBlobContainer(containerName);
                    Console.WriteLine($"Created container {container.Value.Name}");
                }

                return blobContainerClient;
            }
            catch
            {
                Console.WriteLine($"Unable to parse stroage reference or connect to storage account {connectionString}.");
                throw;
            }
        }
    }
}
