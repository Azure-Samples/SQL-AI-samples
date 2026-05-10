// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Create Table",
        ReadOnly = false,
        Destructive = false),
        Description("Creates a new table in the SQL Database. Expects a valid CREATE TABLE SQL statement as input.")]
    public Task<DbOperationResult> CreateTable(
        [Description("CREATE TABLE SQL statement")] string sql,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(sql, conn);
            _ = await cmd.ExecuteNonQueryAsync(ct);
            return DbOperationResult.Ok();
        }, database, cancellationToken, requiresWrite: true);
    }
}
