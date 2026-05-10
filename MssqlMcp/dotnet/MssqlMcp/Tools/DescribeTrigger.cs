// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string DescribeTriggerQuery = @"
SELECT
    tr.object_id AS id,
    tr.name,
    SCHEMA_NAME(t.schema_id) AS table_schema,
    t.name AS table_name,
    tr.is_disabled,
    tr.is_instead_of_trigger,
    OBJECT_DEFINITION(tr.object_id) AS definition,
    STUFF((SELECT ',' + te.type_desc FROM sys.trigger_events te WHERE te.object_id = tr.object_id FOR XML PATH('')), 1, 1, '') AS events
FROM sys.triggers tr
INNER JOIN sys.tables t ON tr.parent_id = t.object_id
WHERE tr.name = @Name AND (SCHEMA_NAME(t.schema_id) = @Schema OR @Schema IS NULL);";

    [McpServerTool(
        Title = "Describe Trigger",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Returns a trigger's definition (T-SQL source), parent table, and bound events.")]
    public Task<DbOperationResult> DescribeTrigger(
        [Description("Trigger name (optionally qualified with parent-table schema, e.g. 'dbo.trg_AfterInsert')")] string name,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        string? schema = null;
        if (name.Contains('.'))
        {
            var parts = name.Split('.', 2);
            schema = parts[0];
            name = parts[1];
        }

        var deny = CheckSchemaAllowed(schema);
        if (deny is not null) return Task.FromResult(deny);

        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(DescribeTriggerQuery, conn);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Schema", (object?)schema ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                return DbOperationResult.Fail($"Trigger '{name}' not found.");
            }

            return DbOperationResult.Ok(new
            {
                id = reader["id"],
                name = reader["name"],
                table_schema = reader["table_schema"],
                table_name = reader["table_name"],
                is_disabled = (bool)reader["is_disabled"],
                is_instead_of = (bool)reader["is_instead_of_trigger"],
                events = reader["events"] is DBNull ? null : reader["events"],
                definition = reader["definition"] is DBNull ? null : reader["definition"],
            });
        }, database, cancellationToken);
    }
}
