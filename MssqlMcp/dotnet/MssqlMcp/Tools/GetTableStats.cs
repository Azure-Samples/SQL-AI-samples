// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string TableStatsQuery = @"
        SELECT
            s.name AS [schema],
            t.name AS [table],
            SUM(p.rows) AS [rowCount],
            SUM(a.total_pages) * 8 AS totalSpaceKB,
            SUM(a.used_pages) * 8 AS usedSpaceKB
        FROM sys.tables t
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        INNER JOIN sys.indexes i ON t.object_id = i.object_id
        INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
        INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
        WHERE i.index_id <= 1
            AND (@TableName IS NULL OR t.name = @TableName)
            AND (@TableSchema IS NULL OR s.name = @TableSchema)
        GROUP BY s.name, t.name
        ORDER BY s.name, t.name";

    [McpServerTool(
        Title = "Get Table Stats",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Returns row counts and space usage statistics for tables in the database.")]
    public async Task<DbOperationResult> GetTableStats(
        [Description("Table name to get stats for (supports 'schema.table' format). If omitted, returns stats for all tables.")] string? name = null)
    {
        string? schema = null;
        if (name != null && name.Contains('.'))
        {
            var parts = name.Split('.');
            if (parts.Length > 1)
            {
                schema = parts[0];
                name = parts[1];
            }
        }

        var conn = await _connectionFactory.GetOpenConnectionAsync();
        try
        {
            using (conn)
            {
                using var cmd = new SqlCommand(TableStatsQuery, conn);
                var _ = cmd.Parameters.AddWithValue("@TableName", name == null ? DBNull.Value : name);
                _ = cmd.Parameters.AddWithValue("@TableSchema", schema == null ? DBNull.Value : schema);

                using var reader = await cmd.ExecuteReaderAsync();
                var stats = new List<object>();
                while (await reader.ReadAsync())
                {
                    stats.Add(new
                    {
                        schema = reader.GetString(0),
                        table = reader.GetString(1),
                        rowCount = reader.GetInt64(2),
                        totalSpaceKB = reader.GetInt64(3),
                        usedSpaceKB = reader.GetInt64(4)
                    });
                }

                return new DbOperationResult(success: true, data: stats);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTableStats failed: {Message}", ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
    }
}
