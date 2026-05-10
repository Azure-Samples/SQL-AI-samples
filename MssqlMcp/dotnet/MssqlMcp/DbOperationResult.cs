// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Data.SqlClient;

namespace Mssql.McpServer;

/// <summary>
/// Represents the result of a database operation, including success status, error message,
/// number of rows affected, returned data, structured SQL error details, and timing.
/// </summary>
public sealed class DbOperationResult
{
    public bool Success { get; init; }

    public string? Error { get; init; }

    public SqlErrorDetails? ErrorDetails { get; init; }

    public int? RowsAffected { get; init; }

    public object? Data { get; init; }

    public long? DurationMs { get; init; }

    public DbOperationResult() { }

    /// <summary>
    /// Backwards-compatible constructor matching the original signature.
    /// </summary>
    public DbOperationResult(bool success, string? error = null, int? rowsAffected = null, object? data = null)
    {
        Success = success;
        Error = error;
        RowsAffected = rowsAffected;
        Data = data;
    }

    public static DbOperationResult Ok(object? data = null, int? rowsAffected = null, long? durationMs = null) =>
        new()
        {
            Success = true,
            Data = data,
            RowsAffected = rowsAffected,
            DurationMs = durationMs,
        };

    public static DbOperationResult Fail(string error, long? durationMs = null) =>
        new()
        {
            Success = false,
            Error = error,
            DurationMs = durationMs,
        };

    public static DbOperationResult FromSqlException(SqlException ex, long? durationMs = null) =>
        new()
        {
            Success = false,
            Error = ex.Message,
            DurationMs = durationMs,
            ErrorDetails = new SqlErrorDetails
            {
                Number = ex.Number,
                State = ex.State,
                Class = ex.Class,
                LineNumber = ex.LineNumber,
                Procedure = string.IsNullOrEmpty(ex.Procedure) ? null : ex.Procedure,
                Server = ex.Server,
            },
        };
}
