// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export.ExportDestinationClient
{
    public class MockExportDestinationClient : IExportDestinationClient
    {
        private Dictionary<Uri, List<Resource>> _exportedData = new Dictionary<Uri, List<Resource>>();
        private readonly Uri _baseUri = new Uri("https://localhost:44348/");

        public async Task<Uri> CreateNewFileAsync(string fileName)
        {
            EnsureArg.IsNotNullOrWhiteSpace(fileName, nameof(fileName));

            var fileUri = new Uri(_baseUri, fileName);
            _exportedData.Add(fileUri, new List<Resource>());

            return await Task.FromResult(fileUri);
        }

        public async Task CommitAsync(Dictionary<Uri, List<Resource>> fileNameToResourcesMapping)
        {
            EnsureArg.IsNotNull(fileNameToResourcesMapping, nameof(fileNameToResourcesMapping));

            foreach (KeyValuePair<Uri, List<Resource>> kvp in fileNameToResourcesMapping)
            {
                List<Resource> resource;
                if (!_exportedData.TryGetValue(kvp.Key, out resource))
                {
                    throw new ArgumentException($"file {kvp.Key} does not exist.");
                }

                resource.AddRange(kvp.Value);
            }

            await Task.CompletedTask;
        }

        public async Task ConnectAsync(string destinationConnectionString)
        {
            EnsureArg.IsNotNullOrWhiteSpace(destinationConnectionString);

            await Task.CompletedTask;
        }
    }
}
