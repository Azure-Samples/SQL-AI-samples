// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string MissingIndexesQuery = @"
SELECT
    DB_NAME(mid.database_id) AS [database],
    OBJECT_SCHEMA_NAME(mid.[object_id], mid.database_id) AS [schema],
    OBJECT_NAME(mid.[object_id], mid.database_id) AS [table],
    migs.user_seeks + migs.user_scans AS uses,
    migs.avg_total_user_cost,
    migs.avg_user_impact,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns,
    'CREATE NONCLUSTERED INDEX [IX_'
        + REPLACE(REPLACE(REPLACE(COALESCE(mid.equality_columns, mid.inequality_columns, ''), ', ', '_'), '[', ''), ']', '')
        + '] ON ' + mid.[statement]
        + ' (' + COALESCE(mid.equality_columns, '')
        + CASE WHEN mid.equality_columns IS NOT NULL AND mid.inequality_columns IS NOT NULL THEN ',' ELSE '' END
        + COALESCE(mid.inequality_columns, '') + ')'
        + CASE WHEN mid.included_columns IS NOT NULL THEN ' INCLUDE (' + mid.included_columns + ')' ELSE '' END AS create_statement,
    (migs.user_seeks + migs.user_scans) * migs.avg_user_impact AS score
FROM sys.dm_db_missing_index_details mid
INNER JOIN sys.dm_db_missing_index_groups mig ON mid.index_handle = mig.index_handle
INNER JOIN sys.dm_db_missing_index_group_stats migs ON mig.index_group_handle = migs.group_handle
WHERE mid.database_id = DB_ID()
  AND (OBJECT_NAME(mid.[object_id], mid.database_id) = @TableName OR @TableName IS NULL)
  AND (OBJECT_SCHEMA_NAME(mid.[object_id], mid.database_id) = @TableSchema OR @TableSchema IS NULL)
ORDER BY score DESC;";

    [McpServerTool(
        Title = "Get Missing Indexes",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Returns SQL Server's missing index suggestions (from DMVs) ranked by impact*usage. Each row includes a ready-to-run CREATE INDEX statement. Suggestions reset when the SQL Server instance restarts.")]
    public Task<DbOperationResult> GetMissingIndexes(
        [Description("Optional table name (or schema.table) to filter results")] string? table = null,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        string? schema = null;
        string? tableName = table;
        if (!string.IsNullOrEmpty(table) && table.Contains('.'))
        {
            var parts = table.Split('.', 2);
            schema = parts[0];
            tableName = parts[1];
        }

        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(MissingIndexesQuery, conn);
            cmd.Parameters.AddWithValue("@TableName", (object?)tableName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TableSchema", (object?)schema ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            var rows = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                rows.Add(new
                {
                    database = reader["database"],
                    schema = reader["schema"],
                    table = reader["table"],
                    uses = reader["uses"],
                    avg_total_user_cost = reader["avg_total_user_cost"],
                    avg_user_impact = reader["avg_user_impact"],
                    equality_columns = reader["equality_columns"] is DBNull ? null : reader["equality_columns"],
                    inequality_columns = reader["inequality_columns"] is DBNull ? null : reader["inequality_columns"],
                    included_columns = reader["included_columns"] is DBNull ? null : reader["included_columns"],
                    create_statement = reader["create_statement"],
                    score = reader["score"],
                });
            }
            return DbOperationResult.Ok(rows);
        }, database, cancellationToken);
    }
}
