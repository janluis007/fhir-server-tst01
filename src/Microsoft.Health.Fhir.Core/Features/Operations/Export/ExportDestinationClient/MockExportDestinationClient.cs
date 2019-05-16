// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export.ExportDestinationClient
{
    public class MockExportDestinationClient : IExportDestinationClient
    {
        private Dictionary<string, List<Resource>> _exportedData = new Dictionary<string, List<Resource>>();

        public async Task CreateNewFileAsync(string fileName)
        {
            EnsureArg.IsNotNullOrWhiteSpace(fileName, nameof(fileName));

            _exportedData.Add(fileName, new List<Resource>());
            await Task.CompletedTask;
        }

        public async Task CommitAsync(Dictionary<string, List<Resource>> fileNameToResourcesMapping)
        {
            EnsureArg.IsNotNull(fileNameToResourcesMapping, nameof(fileNameToResourcesMapping));

            foreach (KeyValuePair<string, List<Resource>> kvp in fileNameToResourcesMapping)
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
