// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Features.Search.Expressions;

namespace Microsoft.Health.Fhir.SqlServer.Features.Search.Expressions.Visitors
{
    /// <summary>
    /// TODO
    /// </summary>
    internal class MultiaryOrRewriter : SqlExpressionRewriter<SearchOptions>
    {
        public static readonly MultiaryOrRewriter Instance = new MultiaryOrRewriter();

        private static readonly SearchParamTableExpression _multiaryOrSearchParamTableExpression = new SearchParamTableExpression(null, null, SearchParamTableExpressionKind.MultiaryOr);

        public override Expression VisitSqlRoot(SqlRootExpression expression, SearchOptions context)
        {
            if (context.CountOnly || expression.SearchParamTableExpressions.Count == 0)
            {
                return expression;
            }

            if (context.Expression is MultiaryExpression multiaryExpression && multiaryExpression.MultiaryOperation == MultiaryOperator.Or)
            {
                var newTableExpressions = new List<SearchParamTableExpression>(expression.SearchParamTableExpressions.Count + 1);
                newTableExpressions.AddRange(expression.SearchParamTableExpressions);

                newTableExpressions.Add(_multiaryOrSearchParamTableExpression);

                return new SqlRootExpression(newTableExpressions, expression.ResourceTableExpressions);
            }

            return expression;
        }
    }
}
