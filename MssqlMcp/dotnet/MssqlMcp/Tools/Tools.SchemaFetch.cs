// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Data.SqlClient;

namespace Mssql.McpServer;

public partial class Tools
{
    internal sealed record ColumnInfo(
        string Name,
        string SqlType,
        short MaxLength,
        byte Precision,
        byte Scale,
        bool Nullable,
        bool IsIdentity,
        bool IsComputed);

    internal sealed record TableSchemaInfo(
        string Schema,
        string Name,
        IReadOnlyList<ColumnInfo> Columns,
        IReadOnlyList<string> PrimaryKey);

    private const string FetchSchemaQuery = @"
DECLARE @ObjectId INT = (
    SELECT t.object_id
    FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = @TableName AND (s.name = @TableSchema OR @TableSchema IS NULL)
);

SELECT s.name AS [schema], t.name AS [name]
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.object_id = @ObjectId;

SELECT c.name, ty.name AS type, c.max_length, c.precision, c.scale,
       c.is_nullable, c.is_identity, c.is_computed
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = @ObjectId
ORDER BY c.column_id;

SELECT c.name
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = @ObjectId AND i.is_primary_key = 1
ORDER BY ic.key_ordinal;
";

    private async Task<TableSchemaInfo?> FetchTableSchemaAsync(SqlConnection conn, string? schema, string name, CancellationToken ct)
    {
        using var cmd = CreateCommand(FetchSchemaQuery, conn);
        cmd.Parameters.AddWithValue("@TableName", name);
        cmd.Parameters.AddWithValue("@TableSchema", (object?)schema ?? DBNull.Value);

        using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
        {
            return null;
        }
        var resolvedSchema = reader.GetString(0);
        var resolvedName = reader.GetString(1);

        await reader.NextResultAsync(ct);
        var columns = new List<ColumnInfo>();
        while (await reader.ReadAsync(ct))
        {
            columns.Add(new ColumnInfo(
                Name: reader.GetString(0),
                SqlType: reader.GetString(1),
                MaxLength: reader.GetInt16(2),
                Precision: reader.GetByte(3),
                Scale: reader.GetByte(4),
                Nullable: reader.GetBoolean(5),
                IsIdentity: reader.GetBoolean(6),
                IsComputed: reader.GetBoolean(7)));
        }

        await reader.NextResultAsync(ct);
        var pk = new List<string>();
        while (await reader.ReadAsync(ct))
        {
            pk.Add(reader.GetString(0));
        }

        return new TableSchemaInfo(resolvedSchema, resolvedName, columns, pk);
    }

    /// <summary>Maps a SQL Server type name (and nullability) to a C# type spelling.</summary>
    internal static string MapSqlTypeToCSharp(string sqlType, bool nullable) =>
        sqlType.ToLowerInvariant() switch
        {
            "bit" => nullable ? "bool?" : "bool",
            "tinyint" => nullable ? "byte?" : "byte",
            "smallint" => nullable ? "short?" : "short",
            "int" => nullable ? "int?" : "int",
            "bigint" => nullable ? "long?" : "long",
            "decimal" or "numeric" or "money" or "smallmoney" => nullable ? "decimal?" : "decimal",
            "float" => nullable ? "double?" : "double",
            "real" => nullable ? "float?" : "float",
            "date" or "datetime" or "datetime2" or "smalldatetime" => nullable ? "DateTime?" : "DateTime",
            "datetimeoffset" => nullable ? "DateTimeOffset?" : "DateTimeOffset",
            "time" => nullable ? "TimeSpan?" : "TimeSpan",
            "uniqueidentifier" => nullable ? "Guid?" : "Guid",
            "binary" or "varbinary" or "image" or "rowversion" or "timestamp" => "byte[]" + (nullable ? "?" : ""),
            "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext" or "xml" or "sysname" => "string" + (nullable ? "?" : ""),
            _ => "object" + (nullable ? "?" : ""),
        };
}
