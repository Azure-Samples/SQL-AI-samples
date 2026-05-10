// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string SearchObjectsQuery = @"
SELECT 'TABLE' AS object_type, s.name AS [schema], t.name AS name, NULL AS parent
FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.name LIKE @Pattern AND (s.name = @Schema OR @Schema IS NULL)
UNION ALL
SELECT 'COLUMN', s.name, c.name, t.name
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE c.name LIKE @Pattern AND (s.name = @Schema OR @Schema IS NULL)
UNION ALL
SELECT 'VIEW', s.name, v.name, NULL
FROM sys.views v INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
WHERE v.name LIKE @Pattern AND (s.name = @Schema OR @Schema IS NULL)
UNION ALL
SELECT 'PROCEDURE', s.name, p.name, NULL
FROM sys.procedures p INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
WHERE p.name LIKE @Pattern AND (s.name = @Schema OR @Schema IS NULL)
UNION ALL
SELECT
    CASE o.type
        WHEN 'FN' THEN 'SCALAR_FUNCTION'
        WHEN 'IF' THEN 'INLINE_TABLE_FUNCTION'
        WHEN 'TF' THEN 'TABLE_FUNCTION'
        WHEN 'FS' THEN 'CLR_SCALAR_FUNCTION'
        WHEN 'FT' THEN 'CLR_TABLE_FUNCTION'
    END,
    s.name, o.name, NULL
FROM sys.objects o INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE o.type IN ('FN','IF','TF','FS','FT') AND o.name LIKE @Pattern AND (s.name = @Schema OR @Schema IS NULL)
ORDER BY object_type, [schema], name;";

    [McpServerTool(
        Title = "Search Objects",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Searches database objects (tables, columns, views, procedures, functions) by name pattern. Use SQL LIKE syntax (% and _).")]
    public Task<DbOperationResult> SearchObjects(
        [Description("Name pattern using SQL LIKE syntax, e.g. '%email%'")] string pattern,
        [Description("Optional schema filter")] string? schema = null,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(SearchObjectsQuery, conn);
            cmd.Parameters.AddWithValue("@Pattern", pattern);
            cmd.Parameters.AddWithValue("@Schema", (object?)schema ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            var results = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                results.Add(new
                {
                    object_type = reader["object_type"],
                    schema = reader["schema"],
                    name = reader["name"],
                    parent = reader["parent"] is DBNull ? null : reader["parent"],
                });
            }
            return DbOperationResult.Ok(results);
        }, database, cancellationToken);
    }
}
