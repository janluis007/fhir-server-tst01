// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections;
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
        private static readonly SqlMetaData[] ResourceTableValuedParameterColumns = { new SqlMetaData("ResourceTypeId", SqlDbType.SmallInt), new SqlMetaData("ResourceBatchOffset", SqlDbType.Int), new SqlMetaData("ResourceId`", SqlDbType.VarChar, 64), new SqlMetaData("RawResource", SqlDbType.VarBinary, SqlMetaData.Max) };
        private static readonly SqlMetaData[] StringSearchParamTableValuedParameterColumns = { new SqlMetaData("ResourceTypeId", SqlDbType.SmallInt), new SqlMetaData("ResourceBatchOffset", SqlDbType.Int), new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("Value", SqlDbType.NVarChar, 512) };
        private static readonly SqlMetaData[] DateSearchParamTableValuedParameterColumns = { new SqlMetaData("ResourceTypeId", SqlDbType.SmallInt), new SqlMetaData("ResourceBatchOffset", SqlDbType.Int), new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("StartTime", SqlDbType.DateTime2), new SqlMetaData("EndTime", SqlDbType.DateTime2) };
        private static readonly SqlMetaData[] ReferenceSearchParamTableValuedParameterColumns = { new SqlMetaData("ResourceTypeId", SqlDbType.SmallInt), new SqlMetaData("ResourceBatchOffset", SqlDbType.Int), new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("BaseUri", SqlDbType.VarChar, 512), new SqlMetaData("ReferenceResourceTypeId", SqlDbType.SmallInt), new SqlMetaData("ReferenceResourceId", SqlDbType.VarChar, 64) };
        private static readonly SqlMetaData[] TokenSearchParamTableValuedParameterColumns = { new SqlMetaData("ResourceTypeId", SqlDbType.SmallInt), new SqlMetaData("ResourceBatchOffset", SqlDbType.Int), new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("System", SqlDbType.NVarChar, 256), new SqlMetaData("Code", SqlDbType.NVarChar, 256) };
        private static readonly SqlMetaData[] TokenTextSearchParamTableValuedParameterColumns = { new SqlMetaData("ResourceTypeId", SqlDbType.SmallInt), new SqlMetaData("ResourceBatchOffset", SqlDbType.Int), new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("Text", SqlDbType.NVarChar, 512) };
        private static readonly SqlMetaData[] QuantitySearchParamTableValuedParameterColumns = { new SqlMetaData("ResourceTypeId", SqlDbType.SmallInt), new SqlMetaData("ResourceBatchOffset", SqlDbType.Int), new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("System", SqlDbType.NVarChar, 256), new SqlMetaData("Code", SqlDbType.NVarChar, 256), new SqlMetaData("Quantity", SqlDbType.Decimal, 18, 6) };
        private static readonly SqlMetaData[] NumberSearchParamTableValuedParameterColumns = { new SqlMetaData("ResourceTypeId", SqlDbType.SmallInt), new SqlMetaData("ResourceBatchOffset", SqlDbType.Int), new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("Number", SqlDbType.Decimal, 18, 6) };
        private static readonly SqlMetaData[] UriSearchParamTableValuedParameterColumns = { new SqlMetaData("ResourceTypeId", SqlDbType.SmallInt), new SqlMetaData("ResourceBatchOffset", SqlDbType.Int), new SqlMetaData("SearchParamId", SqlDbType.SmallInt), new SqlMetaData("CompositeInstanceId", SqlDbType.TinyInt), new SqlMetaData("Uri", SqlDbType.VarChar, 256) };

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

        public Task<UpsertOutcome> UpsertAsync(ResourceWrapper resource, WeakETag weakETag, bool allowCreate, bool keepHistory, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task UpsertManyAsync(IEnumerable<ResourceWrapper> resources, bool allowCreate, bool keepHistory, CancellationToken cancellationToken = default(CancellationToken))
        {
            ////var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    using (var connection = new SqlConnection(_configuration.ConnectionString))
                    {
                        await connection.OpenAsync(cancellationToken);

                        var resourcesAndIndexes = resources.Where(r => r.ResourceTypeName != "Practitioner" && r.ResourceTypeName != "Organization").Select(resourceWrapper => (resourceWrapper, indexEntries: GroupSearchIndexEntriesByType(resourceWrapper.SearchIndices))).ToList();

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandTimeout = 360;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "[dbo].UpsertResource";

                            AddResources(command, resourcesAndIndexes);

                            AddStringSearchParams(resourcesAndIndexes, command);
                            AddTokenSearchParams(resourcesAndIndexes, command);
                            AddDateSearchParams(resourcesAndIndexes, command);

                            AddReferenceSearchParams(resourcesAndIndexes, command);
                            AddQuantitySearchParams(resourcesAndIndexes, command);
                            AddNumberSearchParams(resourcesAndIndexes, command);
                            AddUriSearchParams(resourcesAndIndexes, command);

                            await command.ExecuteScalarAsync(cancellationToken);

                            ////Console.WriteLine(sw.Elapsed + " for " + resourcesAndIndexes.Count + " resources. " + (sw.Elapsed.TotalMilliseconds / resourcesAndIndexes.Count) + " ms / resource");
                            return;
                        }
                    }
                }
                catch (SqlException e) when (e.Number == 1205)
                {
                    Console.WriteLine("Deadlock. Will retry.");
                }
            }
        }

        private void AddResources(SqlCommand command, List<(ResourceWrapper resourceWrapper, ILookup<Type, SearchParameterEntry> indexEntries)> resourcesAndIndexes)
        {
            IEnumerable<SqlDataRecord> Rows()
            {
                int offset = 0;
                foreach (var resourceAndIndex in resourcesAndIndexes)
                {
                    ResourceWrapper resource = resourceAndIndex.resourceWrapper;
                    var r = new SqlDataRecord(ResourceTableValuedParameterColumns);

                    r.SetInt16(0, _resourceTypeToId[resource.ResourceTypeName]);
                    r.SetInt32(1, offset++);
                    r.SetString(2, resource.ResourceId);

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

                            byte[] buffer = ms.GetBuffer();
                            r.SetBytes(3, 0, buffer, 0, (int)ms.Length);

                            yield return r;
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(bytes);
                    }
                }
            }

            var param = command.Parameters.AddWithValue("@tvpResource", NullIfEmpty(Rows()));
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.ResourceTableType";
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

        private static ILookup<Type, SearchParameterEntry> GroupSearchIndexEntriesByType(IReadOnlyCollection<SearchIndexEntry> searchIndexEntries)
        {
            IEnumerable<SearchParameterEntry> Flatten()
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
                                yield return new SearchParameterEntry(searchIndexEntry.SearchParameter, index, compositeInstanceId, componentValue);
                            }
                        }

                        compositeInstanceId++;
                    }
                    else
                    {
                        yield return new SearchParameterEntry(searchIndexEntry.SearchParameter, null, null, searchIndexEntry.Value);
                    }
                }
            }

            return Flatten().ToLookup(e => e.Value.GetType());
        }

        private void AddTokenSearchParams(List<(ResourceWrapper resourceWrapper, ILookup<Type, SearchParameterEntry> indexEntries)> resourcesAndIndexes, SqlCommand command)
        {
            IEnumerable<SqlDataRecord> rows = resourcesAndIndexes.SelectMany((tuple, offset) =>
            {
                var stringEntries = tuple.indexEntries[typeof(TokenSearchValue)]
                    .Where(e => !string.Equals(e.SearchParameter.Name, SearchParameterNames.ResourceType, StringComparison.Ordinal) &&
                                !string.Equals(e.SearchParameter.Name, SearchParameterNames.Id, StringComparison.Ordinal));

                var resourceTypeId = _resourceTypeToId[tuple.resourceWrapper.ResourceTypeName];
                return stringEntries
                    .Where(e =>
                    {
                        var tokenSearchValue = (TokenSearchValue)e.Value;
                        return !string.IsNullOrEmpty(tokenSearchValue.System) || !string.IsNullOrEmpty(tokenSearchValue.Code);
                    })
                    .Select(e =>
                {
                    var r = new SqlDataRecord(TokenSearchParamTableValuedParameterColumns);
                    r.SetInt16(0, resourceTypeId);
                    r.SetInt32(1, offset);

                    r.SetInt16(2, _searchParamUrlToId[(e.SearchParameter.Url, e.ComponentIndex)]);
                    if (e.CompositeInstanceId != null)
                    {
                        r.SetByte(3, e.CompositeInstanceId.Value);
                    }

                    var tokenSearchValue = (TokenSearchValue)e.Value;

                    if (!string.IsNullOrWhiteSpace(tokenSearchValue.System))
                    {
                        r.SetString(4, tokenSearchValue.System);
                    }

                    if (!string.IsNullOrWhiteSpace(tokenSearchValue.Code))
                    {
                        r.SetString(5, tokenSearchValue.Code);
                    }

                    return r;
                });
            });

            SqlParameter param = command.Parameters.AddWithValue("@tvpTokenSearchParam", NullIfEmpty(rows));
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.TokenSearchParamTableType";

            rows = resourcesAndIndexes.SelectMany((tuple, offset) =>
            {
                var stringEntries = tuple.indexEntries[typeof(TokenSearchValue)]
                    .Where(e => !string.Equals(e.SearchParameter.Name, SearchParameterNames.ResourceType, StringComparison.Ordinal) &&
                                !string.Equals(e.SearchParameter.Name, SearchParameterNames.Id, StringComparison.Ordinal));

                var resourceTypeId = _resourceTypeToId[tuple.resourceWrapper.ResourceTypeName];
                return stringEntries
                    .Where(e => e.ComponentIndex == null)
                    .Select(t => (searchParameter: t.SearchParameter, ((TokenSearchValue)t.Value).Text))
                    .Where(t => !string.IsNullOrEmpty(t.Text))
                    .Distinct()
                    .Select(e =>
                    {
                        var r = new SqlDataRecord(TokenTextSearchParamTableValuedParameterColumns);
                        r.SetInt16(0, resourceTypeId);
                        r.SetInt32(1, offset);

                        r.SetInt16(2, _searchParamUrlToId[(e.searchParameter.Url, null)]);
                        r.SetString(3, e.Text);

                        return r;
                    });
            });

            param = command.Parameters.AddWithValue("@tvpTokenTextSearchParam", NullIfEmpty(rows));
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.TokenTextSearchParamTableType";
        }

        private void AddStringSearchParams(List<(ResourceWrapper resourceWrapper, ILookup<Type, SearchParameterEntry> indexEntries)> resourcesAndIndexes, SqlCommand command)
        {
            IEnumerable<SqlDataRecord> rows = resourcesAndIndexes.SelectMany((tuple, offset) =>
            {
                var stringEntries = tuple.indexEntries[typeof(StringSearchValue)];
                var resourceTypeId = _resourceTypeToId[tuple.resourceWrapper.ResourceTypeName];
                return stringEntries.Select(e =>
                {
                    var r = new SqlDataRecord(StringSearchParamTableValuedParameterColumns);
                    r.SetInt16(0, resourceTypeId);
                    r.SetInt32(1, offset);

                    r.SetInt16(2, _searchParamUrlToId[(e.SearchParameter.Url, e.ComponentIndex)]);
                    if (e.CompositeInstanceId != null)
                    {
                        r.SetByte(3, e.CompositeInstanceId.Value);
                    }

                    r.SetString(4, ((StringSearchValue)e.Value).String);
                    return r;
                });
            });

            SqlParameter param = command.Parameters.AddWithValue("@tvpStringSearchParam", NullIfEmpty(rows));
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.StringSearchParamTableType";
        }

        private void AddDateSearchParams(List<(ResourceWrapper resourceWrapper, ILookup<Type, SearchParameterEntry> indexEntries)> resourcesAndIndexes, SqlCommand command)
        {
            IEnumerable<SqlDataRecord> rows = resourcesAndIndexes.SelectMany((tuple, offset) =>
            {
                var stringEntries = tuple.indexEntries[typeof(DateTimeSearchValue)];
                var resourceTypeId = _resourceTypeToId[tuple.resourceWrapper.ResourceTypeName];
                return stringEntries.Select(e =>
                {
                    var r = new SqlDataRecord(DateSearchParamTableValuedParameterColumns);
                    r.SetInt16(0, resourceTypeId);
                    r.SetInt32(1, offset);

                    r.SetInt16(2, _searchParamUrlToId[(e.SearchParameter.Url, e.ComponentIndex)]);
                    if (e.CompositeInstanceId != null)
                    {
                        r.SetByte(3, e.CompositeInstanceId.Value);
                    }

                    r.SetDateTime(4, ((DateTimeSearchValue)e.Value).Start.ToUniversalTime().DateTime);
                    r.SetDateTime(5, ((DateTimeSearchValue)e.Value).End.ToUniversalTime().DateTime);
                    return r;
                });
            });

            SqlParameter param = command.Parameters.AddWithValue("@tvpDateSearchParam", NullIfEmpty(rows));
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.DateSearchParamTableType";
        }

        private void AddReferenceSearchParams(List<(ResourceWrapper resourceWrapper, ILookup<Type, SearchParameterEntry> indexEntries)> resourcesAndIndexes, SqlCommand command)
        {
            IEnumerable<SqlDataRecord> rows = resourcesAndIndexes.SelectMany((tuple, offset) =>
            {
                var stringEntries = tuple.indexEntries[typeof(ReferenceSearchValue)];
                var resourceTypeId = _resourceTypeToId[tuple.resourceWrapper.ResourceTypeName];
                return stringEntries.Select(e =>
                {
                    var referenceSearchValue = (ReferenceSearchValue)e.Value;

                    var r = new SqlDataRecord(ReferenceSearchParamTableValuedParameterColumns);
                    r.SetInt16(0, resourceTypeId);
                    r.SetInt32(1, offset);

                    r.SetInt16(2, _searchParamUrlToId[(e.SearchParameter.Url, e.ComponentIndex)]);
                    if (e.CompositeInstanceId != null)
                    {
                        r.SetByte(3, e.CompositeInstanceId.Value);
                    }

                    if (referenceSearchValue.BaseUri != null)
                    {
                        r.SetString(4, referenceSearchValue.BaseUri.ToString());
                    }

                    if (referenceSearchValue.ResourceType != null)
                    {
                        r.SetInt16(5, _resourceTypeToId[referenceSearchValue.ResourceType.ToString()]);
                    }

                    r.SetString(6, referenceSearchValue.ResourceId);
                    return r;
                });
            });

            SqlParameter referenceParam = command.Parameters.AddWithValue("@tvpReferenceSearchParam", NullIfEmpty(rows));
            referenceParam.SqlDbType = SqlDbType.Structured;
            referenceParam.TypeName = "dbo.ReferenceSearchParamTableType";
        }

        private void AddQuantitySearchParams(List<(ResourceWrapper resourceWrapper, ILookup<Type, SearchParameterEntry> indexEntries)> resourcesAndIndexes, SqlCommand command)
        {
            IEnumerable<SqlDataRecord> rows = resourcesAndIndexes.SelectMany((tuple, offset) =>
            {
                var stringEntries = tuple.indexEntries[typeof(QuantitySearchValue)];
                var resourceTypeId = _resourceTypeToId[tuple.resourceWrapper.ResourceTypeName];
                return stringEntries.Select(e =>
                {
                    var value = (QuantitySearchValue)e.Value;
                    var r = new SqlDataRecord(QuantitySearchParamTableValuedParameterColumns);
                    r.SetInt16(0, resourceTypeId);
                    r.SetInt32(1, offset);

                    r.SetInt16(2, _searchParamUrlToId[(e.SearchParameter.Url, e.ComponentIndex)]);
                    if (e.CompositeInstanceId != null)
                    {
                        r.SetByte(3, e.CompositeInstanceId.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(value.System))
                    {
                        r.SetString(4, value.System);
                    }

                    if (!string.IsNullOrWhiteSpace(value.Code))
                    {
                        r.SetString(5, value.Code);
                    }

                    r.SetDecimal(6, value.Quantity);
                    return r;
                });
            });

            SqlParameter param = command.Parameters.AddWithValue("@tvpQuantitySearchParam", NullIfEmpty(rows));
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.QuantitySearchParamTableType";
        }

        private void AddNumberSearchParams(List<(ResourceWrapper resourceWrapper, ILookup<Type, SearchParameterEntry> indexEntries)> resourcesAndIndexes, SqlCommand command)
        {
            IEnumerable<SqlDataRecord> rows = resourcesAndIndexes.SelectMany((tuple, offset) =>
            {
                var stringEntries = tuple.indexEntries[typeof(NumberSearchValue)];
                var resourceTypeId = _resourceTypeToId[tuple.resourceWrapper.ResourceTypeName];
                return stringEntries.Select(e =>
                {
                    var value = (NumberSearchValue)e.Value;
                    var r = new SqlDataRecord(NumberSearchParamTableValuedParameterColumns);
                    r.SetInt16(0, resourceTypeId);
                    r.SetInt32(1, offset);

                    r.SetInt16(2, _searchParamUrlToId[(e.SearchParameter.Url, e.ComponentIndex)]);
                    if (e.CompositeInstanceId != null)
                    {
                        r.SetByte(3, e.CompositeInstanceId.Value);
                    }

                    r.SetDecimal(4, value.Number);
                    return r;
                });
            });

            SqlParameter param = command.Parameters.AddWithValue("@tvpNumberSearchParam", NullIfEmpty(rows));
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.NumberSearchParamTableType";
        }

        private void AddUriSearchParams(List<(ResourceWrapper resourceWrapper, ILookup<Type, SearchParameterEntry> indexEntries)> resourcesAndIndexes, SqlCommand command)
        {
            IEnumerable<SqlDataRecord> rows = resourcesAndIndexes.SelectMany((tuple, offset) =>
            {
                var stringEntries = tuple.indexEntries[typeof(UriSearchValue)];
                var resourceTypeId = _resourceTypeToId[tuple.resourceWrapper.ResourceTypeName];
                return stringEntries.Select(e =>
                {
                    var value = (UriSearchValue)e.Value;
                    var r = new SqlDataRecord(UriSearchParamTableValuedParameterColumns);
                    r.SetInt16(0, resourceTypeId);
                    r.SetInt32(1, offset);

                    r.SetInt16(2, _searchParamUrlToId[(e.SearchParameter.Url, e.ComponentIndex)]);
                    if (e.CompositeInstanceId != null)
                    {
                        r.SetByte(3, e.CompositeInstanceId.Value);
                    }

                    r.SetString(4, value.Uri);
                    return r;
                });
            });

            SqlParameter param = command.Parameters.AddWithValue("@tvpUriSearchParam", NullIfEmpty(rows));
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

        private static IEnumerable<T> NullIfEmpty<T>(IEnumerable<T> seq)
        {
            IEnumerator<T> enumerator = seq.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return null;
            }

            return new WrappedEnumerable<T>(seq, enumerator);
        }

        private struct SearchParameterEntry
        {
            public SearchParameterEntry(SearchParameter searchParameter, byte? componentIndex, byte? compositeInstanceId, ISearchValue value)
            {
                SearchParameter = searchParameter;
                ComponentIndex = componentIndex;
                CompositeInstanceId = compositeInstanceId;
                Value = value;
            }

            public SearchParameter SearchParameter { get; }

            public byte? ComponentIndex { get; }

            public byte? CompositeInstanceId { get; }

            public ISearchValue Value { get; }
        }

        private class WrappedEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> _original;
            private IEnumerator<T> _startedEnumerator;

            public WrappedEnumerable(IEnumerable<T> original, IEnumerator<T> startedEnumerator)
            {
                _original = original;
                _startedEnumerator = startedEnumerator;
            }

            public IEnumerator<T> GetEnumerator()
            {
                IEnumerable<T> Inner(IEnumerator<T> e)
                {
                    try
                    {
                        do
                        {
                            yield return e.Current;
                        }
                        while (e.MoveNext());
                    }
                    finally
                    {
                        e.Dispose();
                    }
                }

                if (_startedEnumerator != null)
                {
                    IEnumerator<T> e = _startedEnumerator;
                    _startedEnumerator = null;
                    return Inner(e).GetEnumerator();
                }

                return _original.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)this).GetEnumerator();
            }
        }
    }
}
