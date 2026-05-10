// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string ListFunctionsQuery = @"
SELECT s.name AS [schema], o.name, o.type_desc AS type,
       o.create_date, o.modify_date,
       ep.value AS description
FROM sys.objects o
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
LEFT JOIN sys.extended_properties ep ON ep.major_id = o.object_id AND ep.minor_id = 0 AND ep.name = 'MS_Description'
WHERE o.type IN ('FN','IF','TF','FS','FT') AND o.is_ms_shipped = 0
ORDER BY s.name, o.name;";

    [McpServerTool(
        Title = "List Functions",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists all user-defined scalar and table-valued functions in the SQL Database.")]
    public Task<DbOperationResult> ListFunctions(
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(ListFunctionsQuery, conn);
            using var reader = await cmd.ExecuteReaderAsync(ct);
            var functions = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                functions.Add(new
                {
                    schema = reader["schema"],
                    name = reader["name"],
                    type = reader["type"],
                    create_date = reader["create_date"],
                    modify_date = reader["modify_date"],
                    description = reader["description"] is DBNull ? null : reader["description"],
                });
            }
            return DbOperationResult.Ok(functions);
        }, database, cancellationToken);
    }
}
