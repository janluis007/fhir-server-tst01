// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    public sealed class ImportJobTask : IDisposable
    {
        private static readonly int NewLineLength = "\n".Length;

        private readonly ImportJobRecord _importJobRecord;
        private readonly ImportJobConfiguration _importJobConfiguration;
        private readonly IFhirOperationDataStore _fhirOperationDataStore;
        private readonly IResourceWrapperFactory _resourceWrapperFactory;
        private readonly IFhirDataStore _fhirDataStore;
        private readonly FhirJsonParser _fhirJsonParser;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly int _bufferSize;
        private readonly Dictionary<string, IImportProvider> _importProviderDictionary;

        private WeakETag _weakETag;

        public ImportJobTask(
            ImportJobRecord importJobRecord,
            WeakETag weakETag,
            ImportJobConfiguration importJobConfiguration,
            IFhirOperationDataStore fhirOperationDataStore,
            IEnumerable<IImportProvider> importProviders,
            IResourceWrapperFactory resourceWrapperFactory,
            IFhirDataStore fhirDataStore,
            FhirJsonParser fhirParser,
            ILogger<ImportJobTask> logger)
        {
            EnsureArg.IsNotNull(importJobRecord, nameof(importJobRecord));
            EnsureArg.IsNotNull(weakETag, nameof(weakETag));
            EnsureArg.IsNotNull(importJobConfiguration, nameof(importJobConfiguration));
            EnsureArg.IsNotNull(fhirOperationDataStore, nameof(fhirOperationDataStore));
            EnsureArg.IsNotNull(importProviders, nameof(importProviders));
            EnsureArg.IsNotNull(resourceWrapperFactory, nameof(resourceWrapperFactory));
            EnsureArg.IsNotNull(fhirDataStore, nameof(fhirDataStore));
            EnsureArg.IsNotNull(fhirParser, nameof(fhirParser));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _importJobRecord = importJobRecord;
            _importJobConfiguration = importJobConfiguration;
            _fhirOperationDataStore = fhirOperationDataStore;
            _resourceWrapperFactory = resourceWrapperFactory;
            _fhirDataStore = fhirDataStore;
            _fhirJsonParser = fhirParser;
            _logger = logger;

            _bufferSize = _importJobConfiguration.BufferSizeInMbytes * 1024 * 1024;
            _importProviderDictionary = importProviders.ToDictionary(p => p.ProviderType, StringComparer.Ordinal);

            _weakETag = weakETag;
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Try to acquire the job.
                try
                {
                    _logger.LogTrace("Acquiring the job.");

                    await UpdateJobStatus(OperationStatus.Running, cancellationToken);
                }
                catch (JobConflictException)
                {
                    // The job is taken by another process.
                    _logger.LogWarning("Failed to acquire the job. The job was acquired by another process.");
                    return;
                }

                var tasks = new List<Task>();
                int i = 0;

                // Find the entry to work on.
                while (i < _importJobRecord.Request.Input.Count)
                {
                    ImportRequestEntry input = _importJobRecord.Request.Input[i];

                    // Get the current progress. If there is none, create one.
                    ImportJobProgress progress = null;

                    if (_importJobRecord.Progress.Count <= i)
                    {
                        progress = new ImportJobProgress();

                        _importJobRecord.Progress.Add(progress);
                    }
                    else
                    {
                        progress = _importJobRecord.Progress[i];

                        if (progress.IsComplete)
                        {
                            continue;
                        }
                    }

                    var task = Task.Run(() => ProcessInput(input, progress, cancellationToken));

                    tasks.Add(task);
                    i++;

                    if (tasks.Count == _importJobConfiguration.MaximumNumberOfConcurrentTaskPerJob)
                    {
                        Task completedTask = await Task.WhenAny(tasks);

                        if (completedTask.IsFaulted)
                        {
                            _importJobRecord.Errors.TryAdd(new OperationOutcome()
                            {
                                Issue = new System.Collections.Generic.List<OperationOutcome.IssueComponent>()
                                    {
                                        new OperationOutcome.IssueComponent()
                                        {
                                            Diagnostics = completedTask.Exception.ToString(),
                                        },
                                    },
                            });
                        }

                        await UpdateJobStatus(OperationStatus.Running, cancellationToken);

                        tasks.Remove(completedTask);
                    }
                }

                await Task.WhenAll(tasks);

                // We have acquired the job, process the export.
                _logger.LogTrace("Successfully completed the job.");

                _importJobRecord.EndTime = DateTimeOffset.UtcNow;

                await UpdateJobStatus(OperationStatus.Completed, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The job is being terminated.
            }
            catch (Exception ex)
            {
                // The job has encountered an error it cannot recover from.
                // Try to update the job to failed state.
                _logger.LogError(ex, "Encountered an unhandled exception. The job will be marked as failed.");

                _importJobRecord.EndTime = DateTimeOffset.UtcNow;

                await UpdateJobStatus(OperationStatus.Failed, cancellationToken);
            }
        }

        private async Task UpdateJobStatus(OperationStatus operationStatus, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync();

            try
            {
                _importJobRecord.Status = operationStatus;

                ImportJobOutcome updatedImportJobOutcome = await _fhirOperationDataStore.UpdateImportJobAsync(_importJobRecord, _weakETag, cancellationToken);

                _weakETag = updatedImportJobOutcome.ETag;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ProcessInput(ImportRequestEntry input, ImportJobProgress progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Process the file.
            var blobUrl = new Uri(input.Url);

            bool isComplete = false;

            var stopwatch = new Stopwatch();
            var overallStopwatch = new Stopwatch();

            IImportProvider provider = _importProviderDictionary[input.StorageDetail.Type];

            Task<StreamReader> downloadTask = provider.DownloadRangeToStreamReaderAsync(blobUrl, progress.BytesProcessed, _bufferSize, cancellationToken);
            StreamReader streamReader = null;

            do
            {
                stopwatch.Restart();

                streamReader?.Dispose();
                streamReader = await downloadTask;

                _logger.LogInformation("Download range took {Duration}.", stopwatch.ElapsedMilliseconds);

                isComplete = streamReader.BaseStream.Length < _bufferSize;

                string line;

                var tasks = new List<Task>();

                do
                {
                    overallStopwatch.Restart();
                    stopwatch.Restart();

                    line = await streamReader.ReadLineAsync();

                    _logger.LogInformation("Read line took {Duration}.", stopwatch.ElapsedMilliseconds);

                    if (streamReader.EndOfStream && !isComplete)
                    {
                        // Load the next batch.
                        downloadTask = provider.DownloadRangeToStreamReaderAsync(blobUrl, progress.BytesProcessed, _bufferSize, cancellationToken);

                        await Task.WhenAll(tasks);

                        foreach (Task task in tasks)
                        {
                            if (task.IsFaulted)
                            {
                                _importJobRecord.Errors.TryAdd(new OperationOutcome()
                                {
                                    Issue = new System.Collections.Generic.List<OperationOutcome.IssueComponent>()
                                {
                                    new OperationOutcome.IssueComponent()
                                    {
                                        Diagnostics = task.Exception.ToString(),
                                    },
                                },
                                });
                            }
                        }

                        tasks.Clear();

                        // We have reached the end. Commit up to this point.
                        await UpdateJobStatus(OperationStatus.Running, cancellationToken);

                        break;
                    }
                    else
                    {
                        // Upsert the resource.
                        try
                        {
                            stopwatch.Restart();

                            Resource resource = _fhirJsonParser.Parse<Resource>(line);

                            if (resource.Meta == null)
                            {
                                resource.Meta = new Meta();
                            }

                            resource.Meta.LastUpdated = Clock.UtcNow;

                            _logger.LogInformation("Parsing took {Duration}.", stopwatch.ElapsedMilliseconds);

                            stopwatch.Restart();

                            ResourceWrapper wrapper = _resourceWrapperFactory.Create(resource, false);

                            _logger.LogInformation("Creating took {Duration}.", stopwatch.ElapsedMilliseconds);

                            stopwatch.Restart();

                            Task upsertTask = _fhirDataStore.UpsertAsync(wrapper, null, true, true, cancellationToken);

                            tasks.Add(upsertTask);

                            _logger.LogInformation("Upsert took {Duration}.", stopwatch.ElapsedMilliseconds);
                        }
                        catch (Exception ex)
                        {
                            _importJobRecord.Errors.TryAdd(new OperationOutcome()
                            {
                                Issue = new System.Collections.Generic.List<OperationOutcome.IssueComponent>()
                                    {
                                        new OperationOutcome.IssueComponent()
                                        {
                                            Diagnostics = ex.ToString(),
                                        },
                                    },
                            });
                        }

                        progress.Count++;

                        _logger.LogInformation($"Processed {progress.Count}");

                        // Increment the number of bytes processed (including the new line).
                        progress.BytesProcessed += Encoding.UTF8.GetBytes(line).Length + NewLineLength;

                        if (tasks.Count >= 10)
                        {
                            Task completedTask = await Task.WhenAny(tasks);

                            if (completedTask.IsFaulted)
                            {
                                _importJobRecord.Errors.TryAdd(new OperationOutcome()
                                {
                                    Issue = new System.Collections.Generic.List<OperationOutcome.IssueComponent>()
                                    {
                                        new OperationOutcome.IssueComponent()
                                        {
                                            Diagnostics = completedTask.Exception.ToString(),
                                        },
                                    },
                                });
                            }

                            tasks.Remove(completedTask);
                        }
                    }

                    _logger.LogInformation("Overall took {Duration}.", overallStopwatch.ElapsedMilliseconds);
                }
                while (!streamReader.EndOfStream);
            }
            while (!isComplete);

            progress.IsComplete = true;
        }
    }
}
