// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string ListTablesQuery =
        "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES " +
        "WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME";

    [McpServerTool(
        Title = "List Tables",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists all tables in the SQL Database. Optionally targets a specific database.")]
    public Task<DbOperationResult> ListTables(
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(ListTablesQuery, conn);
            var tables = new List<string>();
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
            }
            return DbOperationResult.Ok(tables);
        }, database, cancellationToken);
    }
}
