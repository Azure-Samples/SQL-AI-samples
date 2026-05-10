// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Read Data",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Executes SQL queries against SQL Database to read data. Supports parameterized queries via @name placeholders.")]
    public Task<DbOperationResult> ReadData(
        [Description("SQL query to execute. Use @paramName for parameters.")] string sql,
        [Description("Optional named parameters bound to @paramName placeholders (keys without '@' are accepted)")] Dictionary<string, object?>? parameters = null,
        [Description("Optional per-call cap on returned rows. Capped to the server-wide MAX_ROWS limit.")] int? maxRows = null,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(sql, conn);
            SqlParameterBinding.Apply(cmd, parameters);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            var effectiveCap = Math.Min(maxRows ?? _options.MaxRows, _options.MaxRows);
            var results = new List<Dictionary<string, object?>>();
            var truncated = false;

            while (await reader.ReadAsync(ct))
            {
                if (results.Count >= effectiveCap)
                {
                    truncated = true;
                    break;
                }
                var row = new Dictionary<string, object?>(reader.FieldCount);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                results.Add(row);
            }

            return truncated
                ? DbOperationResult.Ok(new { rows = results, truncated = true, maxRows = effectiveCap })
                : DbOperationResult.Ok(results);
        }, database, cancellationToken);
    }
}
