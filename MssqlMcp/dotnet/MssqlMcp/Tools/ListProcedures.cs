// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string ListProceduresQuery = @"
SELECT s.name AS [schema], p.name, p.create_date, p.modify_date,
       ep.value AS description
FROM sys.procedures p
INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
LEFT JOIN sys.extended_properties ep ON ep.major_id = p.object_id AND ep.minor_id = 0 AND ep.name = 'MS_Description'
WHERE p.is_ms_shipped = 0
ORDER BY s.name, p.name;";

    [McpServerTool(
        Title = "List Procedures",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists all user-defined stored procedures in the SQL Database.")]
    public Task<DbOperationResult> ListProcedures(
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(ListProceduresQuery, conn);
            using var reader = await cmd.ExecuteReaderAsync(ct);
            var procs = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                procs.Add(new
                {
                    schema = reader["schema"],
                    name = reader["name"],
                    create_date = reader["create_date"],
                    modify_date = reader["modify_date"],
                    description = reader["description"] is DBNull ? null : reader["description"],
                });
            }
            return DbOperationResult.Ok(procs);
        }, database, cancellationToken);
    }
}
