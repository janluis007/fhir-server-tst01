// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Core.Features.Conformance;
using Microsoft.Health.Fhir.Core.Features.Definition;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Features.Search.SearchValues;
using Microsoft.IO;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace SandboxImporter
{
    public class SqlServerDataStore : IDataStore, IProvideCapability
    {
        private static readonly SqlMetaData[] StringSearchParamTableValuedParameterColumns = { new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("Value", SqlDbType.NVarChar, 512) };
        private static readonly SqlMetaData[] DateSearchParamTableValuedParameterColumns = { new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("StartTime", SqlDbType.DateTime2), new SqlMetaData("EndTime", SqlDbType.DateTime2) };
        private static readonly SqlMetaData[] ReferenceSearchParamTableValuedParameterColumns = { new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("BaseUri", SqlDbType.VarChar, 512), new SqlMetaData("ReferenceResourceTypeId", SqlDbType.SmallInt), new SqlMetaData("ReferenceResourceId", SqlDbType.VarChar, 64) };
        private static readonly SqlMetaData[] TokenSearchParamTableValuedParameterColumns = { new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("System", SqlDbType.NVarChar, 256), new SqlMetaData("Code", SqlDbType.NVarChar, 256), new SqlMetaData("Text", SqlDbType.NVarChar, 512) };
        private static readonly SqlMetaData[] QuantitySearchParamTableValuedParameterColumns = { new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("System", SqlDbType.NVarChar, 256), new SqlMetaData("Code", SqlDbType.NVarChar, 256), new SqlMetaData("Quantity", SqlDbType.Decimal, 18, 6) };
        private static readonly SqlMetaData[] NumberSearchParamTableValuedParameterColumns = { new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("Number", SqlDbType.Decimal, 18, 6) };
        private static readonly SqlMetaData[] UriSearchParamTableValuedParameterColumns = { new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("Uri", SqlDbType.VarChar, 256) };

        private readonly SqlServerDataStoreConfiguration _configuration;
        private readonly ISearchParameterDefinitionManager _searchParameterDefinitionManager;
        private readonly RecyclableMemoryStreamManager _memoryStreamManager;
        private Dictionary<string, short> _resourceTypeToId;
        private Dictionary<short, string> _resourceTypeIdToTypeName;
        private Dictionary<(string, byte?), short> _searchParamUrlToId;

        public SqlServerDataStore(SqlServerDataStoreConfiguration configuration, ISearchParameterDefinitionManager searchParameterDefinitionManager)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(searchParameterDefinitionManager, nameof(searchParameterDefinitionManager));

            _configuration = configuration;
            _searchParameterDefinitionManager = searchParameterDefinitionManager;
            _memoryStreamManager = new RecyclableMemoryStreamManager();

            InitializeStore().GetAwaiter().GetResult();
        }

        public async Task<UpsertOutcome> UpsertAsync(ResourceWrapper resource, WeakETag weakETag, bool allowCreate, bool keepHistory, CancellationToken cancellationToken = default)
        {
            using (var connection = new SqlConnection(_configuration.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);

                IReadOnlyCollection<SearchIndexEntry> searchIndexEntries = resource.SearchIndices;
                ILookup<Type, (SearchParameter searchParameter, byte? componentIndex, byte? CompositeInstanceId, ISearchValue value)> lookupByType = GroupSearchIndexEntriesByType(searchIndexEntries);

                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "[dbo].UpsertResource";

                    short resourceTypeId = _resourceTypeToId[resource.ResourceTypeName];
                    command.Parameters.AddWithValue("@resourceTypeId", resourceTypeId);
                    command.Parameters.Add(new SqlParameter("@resourceId", SqlDbType.VarChar, 64) { Value = resource.ResourceId });

                    byte[] bytes = ArrayPool<byte>.Shared.Rent(resource.RawResource.Data.Length * 4);
                    try
                    {
                        using (var ms = new RecyclableMemoryStream(_memoryStreamManager))
                        {
                            using (var gzipStream = new GZipStream(ms, CompressionMode.Compress, true))
                            {
                                gzipStream.Write(bytes.AsSpan().Slice(0, Encoding.UTF8.GetBytes(resource.RawResource.Data.AsSpan(), bytes.AsSpan())));
                            }

                            ms.Seek(0, 0);

                            command.Parameters.AddWithValue("@rawResource", ms.GetBuffer()).Size = (int)ms.Length;
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(bytes);
                    }

                    AddStringSearchParams(lookupByType, command);
                    AddTokenSearchParams(lookupByType, command);
                    AddDateSearchParams(lookupByType, command);

                    AddReferenceSearchParams(lookupByType, command);
                    AddQuantitySearchParams(lookupByType, command);
                    AddNumberSearchParams(lookupByType, command);
                    AddUriSearchParams(lookupByType, command);

                    await command.ExecuteScalarAsync(cancellationToken);
                    return new UpsertOutcome(resource, SaveOutcomeType.Created);
                }
            }
        }

        public Task<ResourceWrapper> GetAsync(ResourceKey key, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task HardDeleteAsync(ResourceKey key, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        private async Task InitializeStore()
        {
            using (var connection = new SqlConnection(_configuration.ConnectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand sqlCommand = connection.CreateCommand())
                {
                    sqlCommand.CommandText =
                        @"INSERT INTO dbo.ResourceType (Name) 
                          SELECT value FROM string_split(@p1, ',')
                          EXCEPT SELECT Name from dbo.ResourceType; 
                          
                          SELECT ResourceTypeId, Name FROM dbo.ResourceType;
                
                          INSERT INTO dbo.SearchParam 
                              ([Name], [Uri], [ComponentIndex])
                          SELECT * FROM  OPENJSON (@p2) 
                          WITH ([Name] varchar(200) '$.Name', [Uri] varchar(128) '$.Uri', [ComponentIndex] tinyint '$.ComponentIndex')
                          EXCEPT SELECT Name, Uri, ComponentIndex from dbo.SearchParam;
                
                          SELECT SearchParamId, Uri, ComponentIndex FROM dbo.SearchParam;";

                    sqlCommand.Parameters.AddWithValue("@p1", string.Join(",", ModelInfo.SupportedResources));
                    sqlCommand.Parameters.AddWithValue("@p2", JsonConvert.SerializeObject(GetSearchParameterDefinitions()));

                    using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
                    {
                        _resourceTypeToId = new Dictionary<string, short>(StringComparer.Ordinal);
                        _resourceTypeIdToTypeName = new Dictionary<short, string>();
                        while (await reader.ReadAsync())
                        {
                            string resourceTypeName = reader.GetString(1);
                            short id = reader.GetInt16(0);
                            _resourceTypeToId.Add(resourceTypeName, id);
                            _resourceTypeIdToTypeName.Add(id, resourceTypeName);
                        }

                        await reader.NextResultAsync();

                        _searchParamUrlToId = new Dictionary<(string, byte?), short>();
                        while (await reader.ReadAsync())
                        {
                            _searchParamUrlToId.Add(
                                (reader.GetString(1), reader.IsDBNull(2) ? (byte?)null : reader.GetByte(2)),
                                reader.GetInt16(0));
                        }
                    }
                }
            }
        }

        private static ILookup<Type, (SearchParameter searchParameter, byte? componentIndex, byte? CompositeInstanceId, ISearchValue value)> GroupSearchIndexEntriesByType(IReadOnlyCollection<SearchIndexEntry> searchIndexEntries)
        {
            IEnumerable<(SearchParameter searchParameter, byte? componentIndex, byte? CompositeInstanceId, ISearchValue value)> Flatten()
            {
                byte compositeInstanceId = 0;
                foreach (var searchIndexEntry in searchIndexEntries)
                {
                    if (searchIndexEntry.Value is CompositeSearchValue composite)
                    {
                        for (byte index = 0; index < composite.Components.Count; index++)
                        {
                            foreach (ISearchValue componentValue in composite.Components[index])
                            {
                                yield return (searchIndexEntry.SearchParameter, index, CompositeInstanceId: compositeInstanceId, componentValue);
                            }
                        }

                        compositeInstanceId++;
                    }
                    else
                    {
                        yield return (searchIndexEntry.SearchParameter, null, null, searchIndexEntry.Value);
                    }
                }
            }

            return Flatten().ToLookup(e => e.value.GetType());
        }

        private void AddTokenSearchParams(ILookup<Type, (SearchParameter searchParameter, byte? componentIndex, byte? CompositeInstanceId, ISearchValue value)> lookupByType, SqlCommand command)
        {
            var tokenEntries = lookupByType[typeof(TokenSearchValue)]
                .Where(e => !string.Equals(e.searchParameter.Name, SearchParameterNames.ResourceType, StringComparison.Ordinal) &&
                            !string.Equals(e.searchParameter.Name, SearchParameterNames.Id, StringComparison.Ordinal))
                .Select(e =>
                {
                    string text;
                    var tokenSearchValue = (TokenSearchValue)e.value;

                    if (string.IsNullOrWhiteSpace(tokenSearchValue.Text) || e.componentIndex != null)
                    {
                        // cannot perform text searches on composite params
                        text = null;
                    }
                    else
                    {
                        text = tokenSearchValue.Text; ////.ToUpperInvariant();
                    }

                    return (e.searchParameter, e.componentIndex, e.CompositeInstanceId, tokenSearchValue.System, tokenSearchValue.Code, text);
                })
                .ToList();

            SqlDataRecord[] tokenRecords = tokenEntries.Select(e =>
            {
                var r = new SqlDataRecord(TokenSearchParamTableValuedParameterColumns);
                r.SetInt16(0, _searchParamUrlToId[(e.searchParameter.Url, e.componentIndex)]);
                if (e.CompositeInstanceId != null)
                {
                    r.SetByte(1, e.CompositeInstanceId.Value);
                }

                if (!string.IsNullOrWhiteSpace(e.System))
                {
                    r.SetString(2, e.System);
                }

                if (!string.IsNullOrWhiteSpace(e.Code))
                {
                    r.SetString(3, e.Code);
                }

                if (!string.IsNullOrWhiteSpace(e.text))
                {
                    r.SetString(4, e.text);
                }

                return r;
            }).ToArray();

            SqlParameter param = command.Parameters.AddWithValue("@tvpTokenSearchParam", tokenRecords.Length == 0 ? null : tokenRecords);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.TokenSearchParamTableType";
        }

        private void AddStringSearchParams(ILookup<Type, (SearchParameter searchParameter, byte? componentIndex, byte? CompositeInstanceId, ISearchValue value)> lookupByType, SqlCommand command)
        {
            var stringEntries = lookupByType[typeof(StringSearchValue)].ToList();
            SqlDataRecord[] stringRecords = stringEntries.Select(e =>
            {
                var r = new SqlDataRecord(StringSearchParamTableValuedParameterColumns);
                r.SetInt16(0, _searchParamUrlToId[(e.searchParameter.Url, e.componentIndex)]);
                if (e.CompositeInstanceId != null)
                {
                    r.SetByte(1, e.CompositeInstanceId.Value);
                }

                r.SetString(2, ((StringSearchValue)e.value).String);
                return r;
            }).ToArray();

            SqlParameter param = command.Parameters.AddWithValue("@tvpStringSearchParam", stringRecords.Length == 0 ? null : stringRecords);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.StringSearchParamTableType";
        }

        private void AddDateSearchParams(ILookup<Type, (SearchParameter searchParameter, byte? componentIndex, byte? CompositeInstanceId, ISearchValue value)> lookupByType, SqlCommand command)
        {
            var entries = lookupByType[typeof(DateTimeSearchValue)].ToList();

            SqlDataRecord[] records = entries.Select(e =>
            {
                var r = new SqlDataRecord(DateSearchParamTableValuedParameterColumns);
                r.SetInt16(0, _searchParamUrlToId[(e.searchParameter.Url, e.componentIndex)]);
                if (e.CompositeInstanceId != null)
                {
                    r.SetByte(1, e.CompositeInstanceId.Value);
                }

                r.SetDateTime(2, ((DateTimeSearchValue)e.value).Start.ToUniversalTime().DateTime);
                r.SetDateTime(3, ((DateTimeSearchValue)e.value).End.ToUniversalTime().DateTime);
                return r;
            }).ToArray();

            SqlParameter param = command.Parameters.AddWithValue("@tvpDateSearchParam", records.Length == 0 ? null : records);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.DateSearchParamTableType";
        }

        private void AddReferenceSearchParams(ILookup<Type, (SearchParameter searchParameter, byte? componentIndex, byte? CompositeInstanceId, ISearchValue value)> lookupByType, SqlCommand command)
        {
            var entries = lookupByType[typeof(ReferenceSearchValue)].ToList();

            SqlDataRecord[] referenceParamRecords = entries.Select(e =>
            {
                var referenceSearchValue = (ReferenceSearchValue)e.value;

                var r = new SqlDataRecord(ReferenceSearchParamTableValuedParameterColumns);
                r.SetInt16(0, _searchParamUrlToId[(e.searchParameter.Url, e.componentIndex)]);
                if (e.CompositeInstanceId != null)
                {
                    r.SetByte(1, e.CompositeInstanceId.Value);
                }

                if (referenceSearchValue.BaseUri != null)
                {
                    r.SetString(2, referenceSearchValue.BaseUri.ToString());
                }

                if (referenceSearchValue.ResourceType != null)
                {
                    r.SetInt16(3, _resourceTypeToId[referenceSearchValue.ResourceType.ToString()]);
                }

                r.SetString(4, referenceSearchValue.ResourceId);
                return r;
            }).ToArray();

            SqlParameter referenceParam = command.Parameters.AddWithValue("@tvpReferenceSearchParam", referenceParamRecords.Length == 0 ? null : referenceParamRecords);
            referenceParam.SqlDbType = SqlDbType.Structured;
            referenceParam.TypeName = "dbo.ReferenceSearchParamTableType";
        }

        private void AddQuantitySearchParams(ILookup<Type, (SearchParameter searchParameter, byte? componentIndex, byte? CompositeInstanceId, ISearchValue value)> lookupByType, SqlCommand command)
        {
            var entries = lookupByType[typeof(QuantitySearchValue)].ToList();

            SqlDataRecord[] records = entries.Select(e =>
            {
                var value = (QuantitySearchValue)e.value;
                var r = new SqlDataRecord(QuantitySearchParamTableValuedParameterColumns);
                r.SetInt16(0, _searchParamUrlToId[(e.searchParameter.Url, e.componentIndex)]);
                if (e.CompositeInstanceId != null)
                {
                    r.SetByte(1, e.CompositeInstanceId.Value);
                }

                if (!string.IsNullOrWhiteSpace(value.System))
                {
                    r.SetString(2, value.System);
                }

                if (!string.IsNullOrWhiteSpace(value.Code))
                {
                    r.SetString(3, value.Code);
                }

                r.SetDecimal(4, value.Quantity);
                return r;
            }).ToArray();

            SqlParameter param = command.Parameters.AddWithValue("@tvpQuantitySearchParam", records.Length == 0 ? null : records);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.QuantitySearchParamTableType";
        }

        private void AddNumberSearchParams(ILookup<Type, (SearchParameter searchParameter, byte? componentIndex, byte? CompositeInstanceId, ISearchValue value)> lookupByType, SqlCommand command)
        {
            var entries = lookupByType[typeof(NumberSearchValue)].ToList();

            SqlDataRecord[] records = entries.Select(e =>
            {
                var value = (NumberSearchValue)e.value;
                var r = new SqlDataRecord(NumberSearchParamTableValuedParameterColumns);
                r.SetInt16(0, _searchParamUrlToId[(e.searchParameter.Url, e.componentIndex)]);
                if (e.CompositeInstanceId != null)
                {
                    r.SetByte(1, e.CompositeInstanceId.Value);
                }

                r.SetDecimal(2, value.Number);
                return r;
            }).ToArray();

            SqlParameter param = command.Parameters.AddWithValue("@tvpNumberSearchParam", records.Length == 0 ? null : records);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.NumberSearchParamTableType";
        }

        private void AddUriSearchParams(ILookup<Type, (SearchParameter searchParameter, byte? componentIndex, byte? CompositeInstanceId, ISearchValue value)> lookupByType, SqlCommand command)
        {
            var entries = lookupByType[typeof(UriSearchValue)].ToList();

            SqlDataRecord[] records = entries.Select(e =>
            {
                var value = (UriSearchValue)e.value;
                var r = new SqlDataRecord(UriSearchParamTableValuedParameterColumns);
                r.SetInt16(0, _searchParamUrlToId[(e.searchParameter.Url, e.componentIndex)]);
                if (e.CompositeInstanceId != null)
                {
                    r.SetByte(1, e.CompositeInstanceId.Value);
                }

                r.SetString(2, value.Uri);
                return r;
            }).ToArray();

            SqlParameter param = command.Parameters.AddWithValue("@tvpUriSearchParam", records.Length == 0 ? null : records);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.UriSearchParamTableType";
        }

        public void Build(ListedCapabilityStatement statement)
        {
            EnsureArg.IsNotNull(statement, nameof(statement));

            foreach (var resource in ModelInfo.SupportedResources)
            {
                var resourceType = (ResourceType)Enum.Parse(typeof(ResourceType), resource);
                statement.BuildRestResourceComponent(resourceType, builder =>
                {
                    builder.Versioning.Add(CapabilityStatement.ResourceVersionPolicy.NoVersion);
                    builder.Versioning.Add(CapabilityStatement.ResourceVersionPolicy.Versioned);
                    builder.Versioning.Add(CapabilityStatement.ResourceVersionPolicy.VersionedUpdate);
                    builder.ReadHistory = true;
                    builder.UpdateCreate = true;
                });
            }
        }

        private IEnumerable<dynamic> GetSearchParameterDefinitions()
        {
            foreach (SearchParameter p in _searchParameterDefinitionManager.AllSearchParameters)
            {
                if (p.Type == SearchParamType.Composite)
                {
                    for (int i = 0; i < p.Component.Count; i++)
                    {
                        yield return new { p.Name, Uri = p.Url, ComponentIndex = (int?)i };
                    }
                }
                else
                {
                    yield return new { p.Name, Uri = p.Url, ComponentIndex = (int?)null };
                }
            }
        }
    }
}
