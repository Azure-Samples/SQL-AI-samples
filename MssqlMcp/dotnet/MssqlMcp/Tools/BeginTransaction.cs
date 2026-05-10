// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Begin Transaction",
        ReadOnly = false,
        Destructive = false),
        Description("Opens a transaction on a persisted connection and returns a token. Use the token with ExecuteInTransaction, then CommitTransaction or RollbackTransaction. The connection stays open until commit/rollback.")]
    public async Task<DbOperationResult> BeginTransaction(
        [Description("Optional isolation level: ReadUncommitted, ReadCommitted, RepeatableRead, Serializable, Snapshot. Default: ReadCommitted.")] string? isolationLevel = null,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        if (_options.ReadOnly)
        {
            return DbOperationResult.Fail("Server is in read-only mode. Tool 'BeginTransaction' is blocked.");
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var iso = ParseIsolation(isolationLevel);
            var conn = await _connectionFactory.GetOpenConnectionAsync(database, cancellationToken);
            var tx = (Microsoft.Data.SqlClient.SqlTransaction)await conn.BeginTransactionAsync(iso, cancellationToken);
            var token = _transactionStore.Add(conn, tx);
            sw.Stop();
            return DbOperationResult.Ok(new { transaction_token = token, isolation = iso.ToString() }, durationMs: sw.ElapsedMilliseconds);
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            sw.Stop();
            _logger.LogError(sqlEx, "BeginTransaction failed: {Message}", sqlEx.Message);
            return DbOperationResult.FromSqlException(sqlEx, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "BeginTransaction failed: {Message}", ex.Message);
            return DbOperationResult.Fail(ex.Message, sw.ElapsedMilliseconds);
        }
    }

    private static IsolationLevel ParseIsolation(string? value) => value?.ToLowerInvariant() switch
    {
        "readuncommitted" or "read_uncommitted" => IsolationLevel.ReadUncommitted,
        "repeatableread" or "repeatable_read" => IsolationLevel.RepeatableRead,
        "serializable" => IsolationLevel.Serializable,
        "snapshot" => IsolationLevel.Snapshot,
        _ => IsolationLevel.ReadCommitted,
    };
}
