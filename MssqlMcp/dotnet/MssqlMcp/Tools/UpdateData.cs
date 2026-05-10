// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Update Data",
        ReadOnly = false,
        Destructive = true),
        Description("Updates data in a table. Expects a valid UPDATE statement; supports parameterized values via @name placeholders.")]
    public Task<DbOperationResult> UpdateData(
        [Description("UPDATE SQL statement. Use @paramName for parameters.")] string sql,
        [Description("Optional named parameters bound to @paramName placeholders")] Dictionary<string, object?>? parameters = null,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(sql, conn);
            SqlParameterBinding.Apply(cmd, parameters);
            var rows = await cmd.ExecuteNonQueryAsync(ct);
            return DbOperationResult.Ok(rowsAffected: rows);
        }, database, cancellationToken, requiresWrite: true);
    }
}
