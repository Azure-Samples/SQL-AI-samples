// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json;
using Microsoft.Data.SqlClient;
using Mssql.McpServer;

namespace MssqlMcp.Tests;

/// <summary>
/// Pure unit tests for helper logic that does not require a database.
/// </summary>
public class HelperTests
{
    [Fact]
    public void Unwrap_ReturnsNull_ForNull()
    {
        Assert.Null(SqlParameterBinding.Unwrap(null));
    }

    [Fact]
    public void Unwrap_ReturnsRawClrValue_ForNonJson()
    {
        Assert.Equal(42, SqlParameterBinding.Unwrap(42));
        Assert.Equal("hello", SqlParameterBinding.Unwrap("hello"));
        Assert.Equal(true, SqlParameterBinding.Unwrap(true));
    }

    [Fact]
    public void Unwrap_ConvertsJsonString()
    {
        var je = JsonDocument.Parse("\"hi\"").RootElement;
        Assert.Equal("hi", SqlParameterBinding.Unwrap(je));
    }

    [Fact]
    public void Unwrap_ConvertsJsonNumber_ToInt64WhenPossible()
    {
        var je = JsonDocument.Parse("123").RootElement;
        Assert.Equal(123L, SqlParameterBinding.Unwrap(je));
    }

    [Fact]
    public void Unwrap_ConvertsJsonNumber_ToDoubleForNonInteger()
    {
        var je = JsonDocument.Parse("3.14").RootElement;
        Assert.Equal(3.14, SqlParameterBinding.Unwrap(je));
    }

    [Fact]
    public void Unwrap_ConvertsJsonBooleans()
    {
        Assert.Equal(true, SqlParameterBinding.Unwrap(JsonDocument.Parse("true").RootElement));
        Assert.Equal(false, SqlParameterBinding.Unwrap(JsonDocument.Parse("false").RootElement));
    }

    [Fact]
    public void Unwrap_ReturnsNull_ForJsonNull()
    {
        var je = JsonDocument.Parse("null").RootElement;
        Assert.Null(SqlParameterBinding.Unwrap(je));
    }

    [Fact]
    public void Apply_BindsParameters_WithAtPrefixNormalization()
    {
        using var cmd = new SqlCommand();
        SqlParameterBinding.Apply(cmd, new Dictionary<string, object?>
        {
            { "id", 42 },
            { "@name", "alice" },
        });

        Assert.Equal(2, cmd.Parameters.Count);
        Assert.Equal("@id", cmd.Parameters[0].ParameterName);
        Assert.Equal(42, cmd.Parameters[0].Value);
        Assert.Equal("@name", cmd.Parameters[1].ParameterName);
        Assert.Equal("alice", cmd.Parameters[1].Value);
    }

    [Fact]
    public void Apply_BindsNullAsDbNull()
    {
        using var cmd = new SqlCommand();
        SqlParameterBinding.Apply(cmd, new Dictionary<string, object?> { { "x", null } });
        Assert.Equal(DBNull.Value, cmd.Parameters[0].Value);
    }

    [Fact]
    public void Apply_NoOp_WhenParametersIsNull()
    {
        using var cmd = new SqlCommand();
        SqlParameterBinding.Apply(cmd, null);
        Assert.Empty(cmd.Parameters);
    }

    [Fact]
    public void McpServerOptions_Defaults_AreReasonable()
    {
        var opts = new McpServerOptions();
        Assert.Equal(30, opts.CommandTimeoutSeconds);
        Assert.Equal(1000, opts.MaxRows);
        Assert.False(opts.ReadOnly);
        Assert.Null(opts.AllowedSchemas);
    }

    [Fact]
    public void McpServerOptions_FromEnvironment_ReadsValues()
    {
        Environment.SetEnvironmentVariable("MSSQLMCP_COMMAND_TIMEOUT", "120");
        Environment.SetEnvironmentVariable("MSSQLMCP_MAX_ROWS", "500");
        Environment.SetEnvironmentVariable("MSSQLMCP_READ_ONLY", "true");
        Environment.SetEnvironmentVariable("MSSQLMCP_ALLOWED_SCHEMAS", "dbo, sales ,marketing");
        try
        {
            var opts = McpServerOptions.FromEnvironment();
            Assert.Equal(120, opts.CommandTimeoutSeconds);
            Assert.Equal(500, opts.MaxRows);
            Assert.True(opts.ReadOnly);
            Assert.NotNull(opts.AllowedSchemas);
            Assert.Contains("dbo", opts.AllowedSchemas);
            Assert.Contains("sales", opts.AllowedSchemas);
            Assert.Contains("marketing", opts.AllowedSchemas);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MSSQLMCP_COMMAND_TIMEOUT", null);
            Environment.SetEnvironmentVariable("MSSQLMCP_MAX_ROWS", null);
            Environment.SetEnvironmentVariable("MSSQLMCP_READ_ONLY", null);
            Environment.SetEnvironmentVariable("MSSQLMCP_ALLOWED_SCHEMAS", null);
        }
    }

    [Fact]
    public void DbOperationResult_Ok_HasSuccessTrue()
    {
        var r = DbOperationResult.Ok(data: new { foo = 1 }, rowsAffected: 3, durationMs: 50);
        Assert.True(r.Success);
        Assert.NotNull(r.Data);
        Assert.Equal(3, r.RowsAffected);
        Assert.Equal(50, r.DurationMs);
    }

    [Fact]
    public void DbOperationResult_Fail_CarriesError()
    {
        var r = DbOperationResult.Fail("nope", durationMs: 5);
        Assert.False(r.Success);
        Assert.Equal("nope", r.Error);
        Assert.Equal(5, r.DurationMs);
    }

    [Fact]
    public void DbOperationResult_LegacyConstructor_StillWorks()
    {
        var r = new DbOperationResult(success: true, rowsAffected: 7);
        Assert.True(r.Success);
        Assert.Equal(7, r.RowsAffected);
    }

    [Fact]
    public void TransactionStore_AddGetRemove_Cycle()
    {
        var store = new TransactionStore();
        Assert.Null(store.Get("missing"));
    }
}
