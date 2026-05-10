// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Execute Query",
        ReadOnly = false,
        Destructive = true),
        Description("Generic SQL execution. Returns all result sets (rows, capped) AND rows affected. Use for SELECT/INSERT/UPDATE/DELETE/MERGE, OUTPUT clauses, or multi-statement queries. Supports @name parameters.")]
    public Task<DbOperationResult> ExecuteQuery(
        [Description("SQL statement(s). Use @paramName for parameters.")] string sql,
        [Description("Optional named parameters bound to @paramName placeholders")] Dictionary<string, object?>? parameters = null,
        [Description("Optional per-call cap on returned rows per result set. Capped to the server-wide MAX_ROWS limit.")] int? maxRows = null,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(sql, conn);
            SqlParameterBinding.Apply(cmd, parameters);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            var effectiveCap = Math.Min(maxRows ?? _options.MaxRows, _options.MaxRows);
            var resultSets = new List<object>();

            do
            {
                if (reader.FieldCount == 0)
                {
                    continue;
                }

                var rows = new List<Dictionary<string, object?>>();
                var truncated = false;
                while (await reader.ReadAsync(ct))
                {
                    if (rows.Count >= effectiveCap)
                    {
                        truncated = true;
                        break;
                    }
                    var row = new Dictionary<string, object?>(reader.FieldCount);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    rows.Add(row);
                }

                resultSets.Add(truncated
                    ? new { rows, truncated = true, maxRows = effectiveCap }
                    : new { rows, truncated = false });
            }
            while (await reader.NextResultAsync(ct));

            var rowsAffected = reader.RecordsAffected;
            return DbOperationResult.Ok(
                data: new { resultSets, rowsAffected = rowsAffected < 0 ? (int?)null : rowsAffected },
                rowsAffected: rowsAffected < 0 ? null : rowsAffected);
        }, database, cancellationToken, requiresWrite: true);
    }
}
