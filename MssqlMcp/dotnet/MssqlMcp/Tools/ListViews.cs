// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string ListViewsQuery = @"
SELECT s.name AS [schema], v.name, p.value AS description, v.create_date, v.modify_date
FROM sys.views v
INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
LEFT JOIN sys.extended_properties p ON p.major_id = v.object_id AND p.minor_id = 0 AND p.name = 'MS_Description'
WHERE v.is_ms_shipped = 0
ORDER BY s.name, v.name;";

    [McpServerTool(
        Title = "List Views",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists all user-defined views in the SQL Database.")]
    public Task<DbOperationResult> ListViews(
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(ListViewsQuery, conn);
            using var reader = await cmd.ExecuteReaderAsync(ct);
            var views = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                views.Add(new
                {
                    schema = reader["schema"],
                    name = reader["name"],
                    description = reader["description"] is DBNull ? null : reader["description"],
                    create_date = reader["create_date"],
                    modify_date = reader["modify_date"],
                });
            }
            return DbOperationResult.Ok(views);
        }, database, cancellationToken);
    }
}
