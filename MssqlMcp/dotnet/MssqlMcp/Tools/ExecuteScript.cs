// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private static readonly Regex GoSeparator = new(@"^\s*GO(\s+\d+)?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [McpServerTool(
        Title = "Execute Script",
        ReadOnly = false,
        Destructive = true),
        Description("Executes a multi-batch T-SQL script. Splits on GO separators (line-only). Optionally wraps execution in a single transaction (default: true).")]
    public Task<DbOperationResult> ExecuteScript(
        [Description("Multi-batch T-SQL script. Use 'GO' on its own line to separate batches.")] string script,
        [Description("Wrap execution in a single transaction. If any batch fails, all changes are rolled back. Default: true.")] bool useTransaction = true,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            var batches = SplitGoBatches(script).ToList();
            if (batches.Count == 0)
            {
                return DbOperationResult.Ok(new { batches_executed = 0, total_rows_affected = 0 });
            }

            Microsoft.Data.SqlClient.SqlTransaction? tx = null;
            if (useTransaction)
            {
                tx = (Microsoft.Data.SqlClient.SqlTransaction)await conn.BeginTransactionAsync(ct);
            }

            try
            {
                var totalRows = 0;
                var executed = 0;
                foreach (var batch in batches)
                {
                    using var cmd = CreateCommand(batch, conn, tx);
                    var rows = await cmd.ExecuteNonQueryAsync(ct);
                    if (rows > 0)
                    {
                        totalRows += rows;
                    }
                    executed++;
                }

                if (tx is not null)
                {
                    await tx.CommitAsync(ct);
                }

                return DbOperationResult.Ok(
                    new { batches_executed = executed, total_rows_affected = totalRows },
                    rowsAffected: totalRows);
            }
            catch
            {
                if (tx is not null)
                {
                    try { await tx.RollbackAsync(ct); } catch { /* swallow */ }
                }
                throw;
            }
            finally
            {
                if (tx is not null)
                {
                    await tx.DisposeAsync();
                }
            }
        }, database, cancellationToken, requiresWrite: true);
    }

    private static IEnumerable<string> SplitGoBatches(string script)
    {
        var current = new System.Text.StringBuilder();
        foreach (var rawLine in script.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (GoSeparator.IsMatch(line))
            {
                var batch = current.ToString().Trim();
                if (batch.Length > 0)
                {
                    yield return batch;
                }
                current.Clear();
            }
            else
            {
                current.AppendLine(line);
            }
        }

        var last = current.ToString().Trim();
        if (last.Length > 0)
        {
            yield return last;
        }
    }
}
