﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Fhir.SqlServer.Features.Search.Expressions.Visitors.QueryGenerators
{
    // TODO: FHIBF - Evaluating the usage of this class. At this point I believe it can be deleted.
    internal class UnionAllQueryGenerator : SearchParamTableExpressionQueryGenerator
    {
        internal static readonly UnionAllQueryGenerator Instance = new UnionAllQueryGenerator();

        public override Table Table => null;
    }
}