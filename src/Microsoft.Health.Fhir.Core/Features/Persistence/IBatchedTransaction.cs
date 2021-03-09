// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Health.Abstractions.Features.Transactions;

namespace Microsoft.Health.Fhir.Core.Features.Persistence
{
    public interface IBatchedTransaction : ITransactionScope
    {
        Task CompleteAsync(int expectedItems);
    }
}
