// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Microsoft.Health.Fhir.Store.Export
{
    internal class SqlService : SqlUtils.SqlService
    {
        private byte _partitionId;
        private object _partitioinLocker = new object();
        private byte _numberOfPartitions;

        internal SqlService(string connectionString)
            : base(connectionString, null)
        {
            _numberOfPartitions = 16;
            _partitionId = 0;
        }

        private byte GetNextPartitionId(int? thread)
        {
            if (thread.HasValue)
            {
                return (byte)(thread.Value % _numberOfPartitions);
            }

            lock (_partitioinLocker)
            {
                _partitionId = _partitionId == _numberOfPartitions - 1 ? (byte)0 : ++_partitionId;
                return _partitionId;
            }
        }

        internal bool StoreCopyWorkQueueIsNotEmpty()
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var command = new SqlCommand("SELECT count(*) FROM dbo.StoreCopyWorkQueue", conn) { CommandTimeout = 120 };
            var cnt = (int)command.ExecuteScalar();
            return cnt > 0;
        }

        internal void DequeueStoreCopyWorkQueue(int? thread, out short? resourceTypeId, out byte partitionId, out int unitId, out string minSurIdOrUrl, out string maxSurId)
        {
            resourceTypeId = null;
            partitionId = 0;
            unitId = -1;
            minSurIdOrUrl = string.Empty;
            maxSurId = string.Empty;

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var command = new SqlCommand("dbo.DequeueStoreCopyWorkUnit", conn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 };
            command.Parameters.AddWithValue("@StartPartitionId", GetNextPartitionId(thread));
            command.Parameters.AddWithValue("@Worker", $"{Environment.MachineName}.{Environment.ProcessId}");
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                partitionId = reader.GetByte(0);
                resourceTypeId = reader.GetInt16(1);
                unitId = reader.GetInt32(2);
                minSurIdOrUrl = reader.GetString(3);
                maxSurId = reader.GetString(4);
            }
        }

        internal void PutStoreCopyWorkHeartBeat(byte partitionId, int unitId, int? resourceCount = null)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var command = new SqlCommand("dbo.PutStoreCopyWorkHeartBeat", conn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 };
            command.Parameters.AddWithValue("@PartitionId", partitionId);
            command.Parameters.AddWithValue("@UnitId", unitId);
            if (resourceCount.HasValue)
            {
                command.Parameters.AddWithValue("@ResourceCount", resourceCount.Value);
            }

            command.ExecuteNonQuery();
        }

        internal void CompleteStoreCopyWorkUnit(byte partitionId, int unitId, bool failed, int? resourceCount = null)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var command = new SqlCommand("dbo.PutStoreCopyWorkUnitStatus", conn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 };
            command.Parameters.AddWithValue("@PartitionId", partitionId);
            command.Parameters.AddWithValue("@UnitId", unitId);
            command.Parameters.AddWithValue("@Failed", failed);
            if (resourceCount.HasValue)
            {
                command.Parameters.AddWithValue("@ResourceCount", resourceCount.Value);
            }

            command.ExecuteNonQuery();
        }

        internal IEnumerable<byte[]> GetData(short resourceTypeId, long minId, long maxId)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(
                @$"
SELECT RawResource FROM dbo.Resource WHERE ResourceTypeId = @ResourceTypeId AND ResourceSurrogateId BETWEEN @MinId AND @MaxId AND IsHistory = 0",
                conn)
            { CommandTimeout = 600 };
            cmd.Parameters.AddWithValue("@ResourceTypeId", resourceTypeId);
            cmd.Parameters.AddWithValue("@MinId", minId);
            cmd.Parameters.AddWithValue("@MaxId", maxId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return reader.GetSqlBytes(0).Value;
            }
        }
    }
}
