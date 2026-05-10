// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Execute In Transaction",
        ReadOnly = false,
        Destructive = true),
        Description("Executes a SQL statement on a transaction's persisted connection. Returns rows (if any) and rows affected. Use BeginTransaction first, then CommitTransaction or RollbackTransaction at the end.")]
    public async Task<DbOperationResult> ExecuteInTransaction(
        [Description("Transaction token returned by BeginTransaction")] string transactionToken,
        [Description("SQL statement. Use @paramName for parameters.")] string sql,
        [Description("Optional named parameters bound to @paramName placeholders")] Dictionary<string, object?>? parameters = null,
        [Description("Optional per-call cap on returned rows per result set. Capped to the server-wide MAX_ROWS limit.")] int? maxRows = null,
        CancellationToken cancellationToken = default)
    {
        if (_options.ReadOnly)
        {
            return DbOperationResult.Fail("Server is in read-only mode. Tool 'ExecuteInTransaction' is blocked.");
        }

        var sw = Stopwatch.StartNew();
        var state = _transactionStore.Get(transactionToken);
        if (state is null)
        {
            return DbOperationResult.Fail($"Transaction token '{transactionToken}' not found.");
        }

        try
        {
            using var cmd = CreateCommand(sql, state.Connection, state.Transaction);
            SqlParameterBinding.Apply(cmd, parameters);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
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
                while (await reader.ReadAsync(cancellationToken))
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
            while (await reader.NextResultAsync(cancellationToken));

            var rowsAffected = reader.RecordsAffected;
            sw.Stop();
            return new DbOperationResult
            {
                Success = true,
                Data = new { resultSets, rowsAffected = rowsAffected < 0 ? (int?)null : rowsAffected },
                RowsAffected = rowsAffected < 0 ? null : rowsAffected,
                DurationMs = sw.ElapsedMilliseconds,
            };
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            sw.Stop();
            _logger.LogError(sqlEx, "ExecuteInTransaction failed: {Message}", sqlEx.Message);
            return DbOperationResult.FromSqlException(sqlEx, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ExecuteInTransaction failed: {Message}", ex.Message);
            return DbOperationResult.Fail(ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
