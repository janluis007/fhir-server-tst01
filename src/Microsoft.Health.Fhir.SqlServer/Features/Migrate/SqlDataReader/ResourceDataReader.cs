// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Health.Fhir.SqlServer.Features.Schema;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Fhir.SqlServer.Features.Migrate.SqlDataReader
{
#pragma warning disable CA1063 // Implement IDisposable Correctly
    public class ResourceDataReader : IDataReader
#pragma warning restore CA1063 // Implement IDisposable Correctly
    {
        private List<Dictionary<string, object>> _objectList = null;
        private string _tableName;
        private SchemaInfo _schemaInfo;

        private int _currentIndex = -1;

        public ResourceDataReader(List<Dictionary<string, object>> objectList, string tableName)
        {
            _objectList = objectList;
            _tableName = tableName;
            _schemaInfo = new SchemaManager().GetSchemaWithTableName(_tableName);
        }

        public object this[int i] => _objectList[i];

        public object this[string name] => _objectList[0];

#pragma warning disable SA1201 // Elements should appear in the correct order
        public int Depth
#pragma warning restore SA1201 // Elements should appear in the correct order
        {
            get { return 1; }
        }

        public bool IsClosed
        {
            get { return false; }
        }

        public int RecordsAffected => 0;

        public int FieldCount
        {
            get { return _schemaInfo.SchemaColumns.Count; }
        }

        public void Close()
        {
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            _objectList = null;
        }

        public bool GetBoolean(int i)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotImplementedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotImplementedException();
        }

        public double GetDouble(int i)
        {
            throw new NotImplementedException();
        }

        public Type GetFieldType(int i)
        {
            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            throw new NotImplementedException();
        }

        public int GetInt32(int i)
        {
            throw new NotImplementedException();
        }

        public long GetInt64(int i)
        {
            throw new NotImplementedException();
        }

        public string GetName(int i)
        {
            if (i < _schemaInfo.SchemaColumns.Count)
            {
                return _schemaInfo.SchemaColumns[i];
            }

            return string.Empty;
        }

        public int GetOrdinal(string name)
        {
            if (_schemaInfo.SchemaMapping.ContainsKey(name))
            {
                return _schemaInfo.SchemaMapping[name];
            }

            return -1;
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            throw new NotImplementedException();
        }

        public object GetValue(int i)
        {
            if (_objectList[_currentIndex].ContainsKey(_schemaInfo.SchemaColumns[i]))
            {
                return _objectList[_currentIndex][_schemaInfo.SchemaColumns[i]];
            }

            return null;
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            throw new NotImplementedException();
        }

        public bool NextResult()
        {
            throw new NotImplementedException();
        }

        public bool Read()
        {
            if ((_currentIndex + 1) < _objectList.Count)
            {
                _currentIndex++;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
