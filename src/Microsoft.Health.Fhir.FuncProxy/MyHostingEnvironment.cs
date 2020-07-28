// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting.Internal;

namespace Microsoft.Health.Fhir.FuncProxy
{
    internal class MyHostingEnvironment : HostingEnvironment, IWebHostEnvironment
    {
        public string WebRootPath { get; set; }

        public IFileProvider WebRootFileProvider { get; set; }
    }
}
