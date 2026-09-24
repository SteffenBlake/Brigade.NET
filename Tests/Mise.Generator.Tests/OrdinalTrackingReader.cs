using System.Collections;
using System.Data;
using System.Data.Common;

namespace Brigade.Net.Mise.Generator.Tests;

public sealed class OrdinalTrackingReader(DbDataReader inner, string[]? projectedNames = null) : DbDataReader
{
    public int NameLookupCount { get; private set; }

    public int TypedReadCount { get; private set; }

    public bool BindingComplete { get; set; }

    public override object this[int ordinal] => inner[ordinal];

    public override object this[string name] => inner[name];

    public override int Depth => inner.Depth;

    public override int FieldCount => inner.FieldCount;

    public override bool HasRows => inner.HasRows;

    public override bool IsClosed => inner.IsClosed;

    public override int RecordsAffected => inner.RecordsAffected;

    public override bool GetBoolean(int ordinal) => inner.GetBoolean(ordinal);

    public override byte GetByte(int ordinal) => inner.GetByte(ordinal);

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        return inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
    }

    public override char GetChar(int ordinal) => inner.GetChar(ordinal);

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        return inner.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
    }

    public override string GetDataTypeName(int ordinal) => inner.GetDataTypeName(ordinal);

    public override DateTime GetDateTime(int ordinal) => inner.GetDateTime(ordinal);

    public override decimal GetDecimal(int ordinal) => inner.GetDecimal(ordinal);

    public override double GetDouble(int ordinal) => inner.GetDouble(ordinal);

    public override IEnumerator GetEnumerator() => ((IEnumerable)inner).GetEnumerator();

    public override Type GetFieldType(int ordinal) => inner.GetFieldType(ordinal);

    public override T GetFieldValue<T>(int ordinal)
    {
        TypedReadCount++;
        return inner.GetFieldValue<T>(ordinal);
    }

    public override float GetFloat(int ordinal) => inner.GetFloat(ordinal);

    public override Guid GetGuid(int ordinal) => inner.GetGuid(ordinal);

    public override short GetInt16(int ordinal) => inner.GetInt16(ordinal);

    public override int GetInt32(int ordinal) => inner.GetInt32(ordinal);

    public override long GetInt64(int ordinal) => inner.GetInt64(ordinal);

    public override string GetName(int ordinal)
    {
        if (BindingComplete)
        {
            throw new InvalidOperationException("Column name was read after ordinal binding.");
        }

        NameLookupCount++;
        return projectedNames is null ? inner.GetName(ordinal) : projectedNames[ordinal];
    }

    public override int GetOrdinal(string name) => inner.GetOrdinal(name);

    public override DataTable? GetSchemaTable() => inner.GetSchemaTable();

    public override string GetString(int ordinal) => inner.GetString(ordinal);

    public override object GetValue(int ordinal) => inner.GetValue(ordinal);

    public override int GetValues(object[] values) => inner.GetValues(values);

    public override bool IsDBNull(int ordinal) => inner.IsDBNull(ordinal);

    public override bool NextResult() => inner.NextResult();

    public override bool Read() => inner.Read();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
