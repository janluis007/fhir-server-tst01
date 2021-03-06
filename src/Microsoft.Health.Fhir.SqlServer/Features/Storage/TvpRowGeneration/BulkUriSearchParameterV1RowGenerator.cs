// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Core.Features.Search.SearchValues;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Fhir.SqlServer.Features.Storage.TvpRowGeneration
{
    internal class BulkUriSearchParameterV1RowGenerator : BulkSearchParameterRowGenerator<UriSearchValue, BulkUriSearchParamTableTypeV1Row>
    {
        public BulkUriSearchParameterV1RowGenerator(SqlServerFhirModel model, SearchParameterToSearchValueTypeMap searchParameterTypeMap)
            : base(model, searchParameterTypeMap)
        {
        }

        internal override bool TryGenerateRow(int offset, short searchParamId, UriSearchValue searchValue, out BulkUriSearchParamTableTypeV1Row row)
        {
            row = new BulkUriSearchParamTableTypeV1Row(offset, searchParamId, searchValue.Uri);
            return true;
        }
    }
}
