// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

[McpServerToolType]
public partial class Tools
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<Tools> _logger;
    private readonly McpServerOptions _options;
    private readonly TransactionStore _transactionStore;

    public Tools(
        ISqlConnectionFactory connectionFactory,
        ILogger<Tools> logger,
        McpServerOptions? options = null,
        TransactionStore? transactionStore = null)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _options = options ?? new McpServerOptions();
        _transactionStore = transactionStore ?? new TransactionStore();
    }

    /// <summary>
    /// Centralizes connection lifecycle, timing, and structured error handling for every tool.
    /// The lambda receives an open connection and the cancellation token; it returns the operation
    /// result. Exceptions are translated to <see cref="DbOperationResult"/> failures.
    /// </summary>
    private async Task<DbOperationResult> ExecuteAsync(
        Func<SqlConnection, CancellationToken, Task<DbOperationResult>> operation,
        string? database = null,
        CancellationToken cancellationToken = default,
        bool requiresWrite = false,
        [CallerMemberName] string operationName = "")
    {
        if (requiresWrite && _options.ReadOnly)
        {
            _logger.LogWarning("{Operation} blocked: server is in read-only mode (MSSQLMCP_READ_ONLY=true).", operationName);
            return DbOperationResult.Fail($"Server is in read-only mode. Tool '{operationName}' is blocked.");
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await using var conn = await _connectionFactory.GetOpenConnectionAsync(database, cancellationToken);
            var result = await operation(conn, cancellationToken);
            sw.Stop();

            _logger.LogInformation(
                "{Operation} completed in {Ms}ms (success: {Success}, rows: {Rows})",
                operationName, sw.ElapsedMilliseconds, result.Success, result.RowsAffected);

            return result.DurationMs is not null
                ? result
                : new DbOperationResult
                {
                    Success = result.Success,
                    Error = result.Error,
                    ErrorDetails = result.ErrorDetails,
                    RowsAffected = result.RowsAffected,
                    Data = result.Data,
                    DurationMs = sw.ElapsedMilliseconds,
                };
        }
        catch (SqlException sqlEx)
        {
            sw.Stop();
            _logger.LogError(
                sqlEx,
                "{Operation} failed (SQL {Number}/{State} line {Line}): {Message}",
                operationName, sqlEx.Number, sqlEx.State, sqlEx.LineNumber, sqlEx.Message);
            return DbOperationResult.FromSqlException(sqlEx, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("{Operation} cancelled after {Ms}ms", operationName, sw.ElapsedMilliseconds);
            return DbOperationResult.Fail("Operation cancelled.", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "{Operation} failed: {Message}", operationName, ex.Message);
            return DbOperationResult.Fail(ex.Message, sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Returns a failure result if the requested schema is not in the configured allowlist.
    /// Returns null when the call is allowed (no allowlist configured, or schema is null/allowed).
    /// </summary>
    private DbOperationResult? CheckSchemaAllowed(string? schema)
    {
        if (_options.AllowedSchemas is null || string.IsNullOrEmpty(schema))
        {
            return null;
        }
        return _options.AllowedSchemas.Contains(schema)
            ? null
            : DbOperationResult.Fail($"Schema '{schema}' is not in MSSQLMCP_ALLOWED_SCHEMAS.");
    }

    /// <summary>
    /// Creates a <see cref="SqlCommand"/> with the configured command timeout (and an optional transaction).
    /// </summary>
    private SqlCommand CreateCommand(string sql, SqlConnection conn, SqlTransaction? tx = null)
    {
        var cmd = new SqlCommand(sql, conn)
        {
            CommandTimeout = _options.CommandTimeoutSeconds,
        };
        if (tx is not null)
        {
            cmd.Transaction = tx;
        }
        return cmd;
    }
}
