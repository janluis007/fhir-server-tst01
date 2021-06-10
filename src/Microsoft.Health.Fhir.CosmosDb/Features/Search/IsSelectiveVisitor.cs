// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Core.Features.Search.Expressions;
using Microsoft.Health.Fhir.ValueSets;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Search
{
    internal class IsSelectiveVisitor : IExpressionVisitor<IsSelectiveVisitor.IsSelectiveVisitorContext, object>
    {
        public object VisitSearchParameter(SearchParameterExpression expression, IsSelectiveVisitorContext context)
        {
            context.SearchParameterUris.Add(expression.Parameter.Url);

            // identifiers should be searching for a very limited number of records
            if (expression.Parameter.Type == SearchParamType.Token && string.Equals(expression.Parameter.Code, "identifier", StringComparison.Ordinal))
            {
                context.SelectiveParametersUsed = true;
            }

            return null;
        }

        public object VisitBinary(BinaryExpression expression, IsSelectiveVisitorContext context) => default;

        public object VisitChained(ChainedExpression expression, IsSelectiveVisitorContext context) => default;

        public object VisitMissingField(MissingFieldExpression expression, IsSelectiveVisitorContext context) => default;

        public object VisitMissingSearchParameter(MissingSearchParameterExpression expression, IsSelectiveVisitorContext context) => default;

        public object VisitNotExpression(NotExpression expression, IsSelectiveVisitorContext context)
        {
            return expression.Expression.AcceptVisitor(this, context);
        }

        public object VisitMultiary(MultiaryExpression expression, IsSelectiveVisitorContext context)
        {
            MultiaryOperator op = expression.MultiaryOperation;
            IReadOnlyList<Expression> expressions = expression.Expressions;

            for (int i = 0; i < expressions.Count; i++)
            {
                // Output each expression.
                expressions[i].AcceptVisitor(this, context);
            }

            return null;
        }

        public object VisitString(StringExpression expression, IsSelectiveVisitorContext context) => default;

        public object VisitCompartment(CompartmentSearchExpression expression, IsSelectiveVisitorContext context) => default;

        public object VisitInclude(IncludeExpression expression, IsSelectiveVisitorContext context) => default;

        public object VisitSortParameter(SortExpression expression, IsSelectiveVisitorContext context) => default;

        internal class IsSelectiveVisitorContext
        {
            public ISet<Uri> SearchParameterUris { get; } = new HashSet<Uri>();

            public bool SelectiveParametersUsed { get; set; }
        }
    }
}
