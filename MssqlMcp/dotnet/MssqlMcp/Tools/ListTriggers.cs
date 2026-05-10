// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string ListTriggersQuery = @"
SELECT
    SCHEMA_NAME(t.schema_id) AS table_schema,
    t.name AS table_name,
    tr.name AS trigger_name,
    tr.is_disabled,
    tr.is_instead_of_trigger,
    STUFF((SELECT ',' + te.type_desc FROM sys.trigger_events te WHERE te.object_id = tr.object_id FOR XML PATH('')), 1, 1, '') AS events
FROM sys.triggers tr
INNER JOIN sys.tables t ON tr.parent_id = t.object_id
WHERE tr.is_ms_shipped = 0
  AND (t.name = @TableName OR @TableName IS NULL)
  AND (SCHEMA_NAME(t.schema_id) = @TableSchema OR @TableSchema IS NULL)
ORDER BY table_schema, table_name, trigger_name;";

    [McpServerTool(
        Title = "List Triggers",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists DML triggers, optionally filtered by table.")]
    public Task<DbOperationResult> ListTriggers(
        [Description("Optional table name (or schema.table) to filter triggers")] string? table = null,
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
            using var cmd = CreateCommand(ListTriggersQuery, conn);
            cmd.Parameters.AddWithValue("@TableName", (object?)tableName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TableSchema", (object?)schema ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            var triggers = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                triggers.Add(new
                {
                    table_schema = reader["table_schema"],
                    table_name = reader["table_name"],
                    trigger_name = reader["trigger_name"],
                    is_disabled = (bool)reader["is_disabled"],
                    is_instead_of = (bool)reader["is_instead_of_trigger"],
                    events = reader["events"] is DBNull ? null : reader["events"],
                });
            }
            return DbOperationResult.Ok(triggers);
        }, database, cancellationToken);
    }
}
