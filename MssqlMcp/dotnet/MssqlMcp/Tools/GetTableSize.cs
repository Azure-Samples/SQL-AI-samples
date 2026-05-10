// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string TableSizeQuery = @"
SELECT
    s.name AS [schema],
    t.name AS [table],
    SUM(CASE WHEN p.index_id IN (0, 1) THEN p.[rows] ELSE 0 END) AS row_count,
    SUM(a.total_pages) * 8 AS total_kb,
    SUM(a.used_pages) * 8 AS used_kb,
    SUM(a.data_pages) * 8 AS data_kb,
    (SUM(a.used_pages) - SUM(a.data_pages)) * 8 AS index_kb,
    (SUM(a.total_pages) - SUM(a.used_pages)) * 8 AS unused_kb
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN sys.partitions p ON t.object_id = p.object_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE (t.name = @TableName OR @TableName IS NULL)
  AND (s.name = @TableSchema OR @TableSchema IS NULL)
GROUP BY s.name, t.name
ORDER BY total_kb DESC;";

    [McpServerTool(
        Title = "Get Table Size",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Returns row count and storage size (KB) for a table. Omit name to list all tables sorted by size.")]
    public Task<DbOperationResult> GetTableSize(
        [Description("Optional table name (or schema.table). Leave empty to list all tables.")] string? name = null,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        string? schema = null;
        string? tableName = name;
        if (!string.IsNullOrEmpty(name) && name.Contains('.'))
        {
            var parts = name.Split('.', 2);
            schema = parts[0];
            tableName = parts[1];
        }

        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(TableSizeQuery, conn);
            cmd.Parameters.AddWithValue("@TableName", (object?)tableName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TableSchema", (object?)schema ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            var rows = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                rows.Add(new
                {
                    schema = reader["schema"],
                    table = reader["table"],
                    row_count = reader["row_count"],
                    total_kb = reader["total_kb"],
                    used_kb = reader["used_kb"],
                    data_kb = reader["data_kb"],
                    index_kb = reader["index_kb"],
                    unused_kb = reader["unused_kb"],
                });
            }
            return DbOperationResult.Ok(rows);
        }, database, cancellationToken);
    }
}
