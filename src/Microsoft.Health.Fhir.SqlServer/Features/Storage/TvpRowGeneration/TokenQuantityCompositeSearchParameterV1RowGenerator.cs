// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Core.Features.Search.SearchValues;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Fhir.SqlServer.Features.Storage.TvpRowGeneration
{
    internal class TokenQuantityCompositeSearchParameterV1RowGenerator : CompositeSearchParameterRowGenerator<(TokenSearchValue component1, QuantitySearchValue component2), TokenQuantityCompositeSearchParamTableTypeV1Row>
    {
        private readonly TokenSearchParameterV1RowGenerator _tokenRowGenerator;
        private readonly QuantitySearchParameterV1RowGenerator _quantityV1RowGenerator;

        public TokenQuantityCompositeSearchParameterV1RowGenerator(SqlServerFhirModel model, TokenSearchParameterV1RowGenerator tokenRowGenerator, QuantitySearchParameterV1RowGenerator quantityV1RowGenerator)
            : base(model)
        {
            _tokenRowGenerator = tokenRowGenerator;
            _quantityV1RowGenerator = quantityV1RowGenerator;
        }

        internal override bool TryGenerateRow(short searchParamId, (TokenSearchValue component1, QuantitySearchValue component2) searchValue, out TokenQuantityCompositeSearchParamTableTypeV1Row row)
        {
            if (_tokenRowGenerator.TryGenerateRow(default, searchValue.component1, out var token1Row) &&
                _quantityV1RowGenerator.TryGenerateRow(default, searchValue.component2, out var token2Row))
            {
                row = new TokenQuantityCompositeSearchParamTableTypeV1Row(
                    searchParamId,
                    token1Row.SystemId,
                    token1Row.Code,
                    token2Row.SystemId,
                    token2Row.QuantityCodeId,
                    token2Row.SingleValue,
                    token2Row.LowValue,
                    token2Row.HighValue);

                return true;
            }

            row = default;
            return false;
        }
    }
}
