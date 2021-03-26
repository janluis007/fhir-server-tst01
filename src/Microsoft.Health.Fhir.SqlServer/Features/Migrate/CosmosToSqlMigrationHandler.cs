// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Fhir.Core.Features.Operations;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.SqlServer.Features.Migrate.SqlDataReader;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;
using Microsoft.Health.Fhir.SqlServer.Features.Storage;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.Fhir.SqlServer.Features.Migrate
{
    internal class CosmosToSqlMigrationHandler : IFhirDataMigrationHandler
    {
        private readonly SqlServerFhirModel _model;
        private VLatest.UpsertResourceTvpGenerator<ResourceMetadata> _upsertResourceTvpGeneratorVLatest;
        private SearchParameterToSearchValueTypeMap _searchParameterTypeMap;
        private readonly SqlServerDataStoreConfiguration _sqlServerConfig;
        private long _nextSurrogateId = 0;

        public CosmosToSqlMigrationHandler(
            SqlServerFhirModel model,
            SqlServerDataStoreConfiguration sqlServerConfig,
            VLatest.UpsertResourceTvpGenerator<ResourceMetadata> upsertResourceTvpGeneratorVLatest,
            SearchParameterToSearchValueTypeMap searchParameterToSearchValueTypeMap)
        {
            _model = model;
            _sqlServerConfig = sqlServerConfig;
            _upsertResourceTvpGeneratorVLatest = upsertResourceTvpGeneratorVLatest;
            _searchParameterTypeMap = searchParameterToSearchValueTypeMap;
        }

        private long NextSurrogateId
        {
            get
            {
                _nextSurrogateId += 1;
                if (_nextSurrogateId > 8000)
                {
                    _nextSurrogateId = 1;
                }

                return _nextSurrogateId - 1;
            }
        }

        public Task Process(IEnumerable<ResourceWrapper> resourceList)
        {
            var resourceValueList = new List<Dictionary<string, object>>();
            var searchParamValueList = new Dictionary<string, List<Dictionary<string, object>>>();
            foreach (var resource in resourceList)
            {
                long baseResourceSurrogateId = ResourceSurrogateIdHelper.LastUpdatedToResourceSurrogateId(resource.LastModified.UtcDateTime);
                long resourceSurrogateId = baseResourceSurrogateId + NextSurrogateId;
                var valueSet = GetReourceValueSet(resource, resourceSurrogateId);
                resourceValueList.Add(valueSet);

                ExtractSearchParameterValues(searchParamValueList, resource, resourceSurrogateId);
            }

            SaveToSqlServer(resourceValueList, "Resource");
            foreach (var kvp in searchParamValueList)
            {
                SaveToSqlServer(kvp.Value, kvp.Key);
            }

            return Task.CompletedTask;
        }

        private void ExtractSearchParameterValues(Dictionary<string, List<Dictionary<string, object>>> valueMap, ResourceWrapper resource, long resourceSurrogateId)
        {
            short resourceTypeId = _model.GetResourceTypeId(resource.ResourceTypeName);
            bool isHistory = resource.IsHistory;

            var resourceMetadata = new ResourceMetadata(
                resource.CompartmentIndices,
                resource.SearchIndices?.ToLookup(e => _searchParameterTypeMap.GetSearchValueType(e)),
                resource.LastModifiedClaims);
            var searchParameterValues = _upsertResourceTvpGeneratorVLatest.Generate(resourceMetadata);
            var properties = searchParameterValues.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var property in searchParameterValues.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var paramValues = property.GetValue(searchParameterValues);

                IEnumerable paramValueList = (IEnumerable)paramValues;
                foreach (var rowValue in paramValueList)
                {
                    var rowType = rowValue.GetType();
                    var paramName = rowType.Name;
                    var paramKey = paramName.Substring(0, paramName.IndexOf("TableType", StringComparison.OrdinalIgnoreCase));
                    if (!valueMap.ContainsKey(paramKey))
                    {
                        valueMap[paramKey] = new List<Dictionary<string, object>>();
                    }

                    var parameterMap = new Dictionary<string, object>
                    {
                        { "ResourceSurrogateId", resourceSurrogateId },
                        { "ResourceTypeId", resourceTypeId },
                        { "IsHistory", isHistory },
                    };

                    foreach (var columnProperty in rowType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        var columnValue = columnProperty.GetValue(rowValue);
                        if (columnValue is DateTimeOffset dateTimeOffset)
                        {
                            parameterMap[columnProperty.Name] = dateTimeOffset.UtcDateTime;
                        }
                        else
                        {
                            parameterMap[columnProperty.Name] = columnValue;
                        }
                    }

                    valueMap[paramKey].Add(parameterMap);
                }
            }
        }

        private void SaveToSqlServer(List<Dictionary<string, object>> valueList, string tableName)
        {
            var reader = new ResourceDataReader(valueList, tableName);
            using (SqlConnection destinationConnection = new SqlConnection(_sqlServerConfig.ConnectionString))
            {
                destinationConnection.Open();

                // Set up the bulk copy object.
                // Note that the column positions in the source
                // data reader match the column positions in
                // the destination table so there is no need to
                // map columns.
                using (SqlBulkCopy bulkCopy =
                            new SqlBulkCopy(destinationConnection))
                {
                    bulkCopy.DestinationTableName =
                        $"dbo.{tableName}";

                    try
                    {
                        // Write from the source to the destination.
                        bulkCopy.WriteToServer(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
            }
        }

        private Dictionary<string, object> GetReourceValueSet(ResourceWrapper resource, long resourceSurrogateId)
        {
            var values = new Dictionary<string, object>();

            long baseResourceSurrogateId = ResourceSurrogateIdHelper.LastUpdatedToResourceSurrogateId(resource.LastModified.UtcDateTime);
            short resourceTypeId = _model.GetResourceTypeId(resource.ResourceTypeName);

            values["ResourceSurrogateId"] = resourceSurrogateId;
            values["ResourceTypeId"] = resourceTypeId;
            values["Version"] = int.Parse(resource.Version);
            values["IsHistory"] = resource.IsHistory;
            values["ResourceId"] = resource.ResourceId;
            values["IsDeleted"] = resource.IsDeleted;
            values["RequestMethod"] = resource.Request.Method;
            values["IsRawResourceMetaSet"] = resource.RawResource.IsMetaSet;
            using (MemoryStream s = new MemoryStream())
            {
                CompressedRawResourceConverter.WriteCompressedRawResource(s, resource.RawResource.Data);
                values["RawResource"] = s.ToArray();
            }

            return values;
        }
    }
}
