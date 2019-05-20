// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Export.ExportDestinationClient
{
    public class ExportDestinationClientFactory
    {
        private static readonly Lazy<ExportDestinationClientFactory> _instance = new Lazy<ExportDestinationClientFactory>(() => new ExportDestinationClientFactory());
        private Dictionary<string, Func<IExportDestinationClient>> _registeredTypes;

        private ExportDestinationClientFactory()
        {
            _registeredTypes = new Dictionary<string, Func<IExportDestinationClient>>(StringComparer.Ordinal);
            _registeredTypes.Add("Mock", () => new MockExportDestinationClient());
        }

        public static ExportDestinationClientFactory Instance => _instance.Value;

        public IExportDestinationClient GetExportDestinationClient(string destinationType)
        {
            EnsureArg.IsNotNullOrWhiteSpace(destinationType, nameof(destinationType));

            if (_registeredTypes.TryGetValue(destinationType, out Func<IExportDestinationClient> ctor))
            {
                return ctor();
            }

            return new MockExportDestinationClient();
        }
    }
}
