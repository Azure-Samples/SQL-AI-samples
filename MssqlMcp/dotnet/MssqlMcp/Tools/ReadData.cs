// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;
public partial class Tools
{
    [McpServerTool(
        Title = "Read Workspace DBData",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Executes SQL queries against SQL Database to read data")]
    public async Task<DbOperationResult> ReadWorkspaceDBData(
        [Description("SQL query to execute")] string sql)
    {
        return await ReadDBData(sql, "WORKSPACE_CONNECTION_STRING");
    }

    [McpServerTool(
        Title = "Read EDDS DBData",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Executes SQL queries against SQL Database to read data")]
    public async Task<DbOperationResult> ReadEddsDBData(
        [Description("SQL query to execute")] string sql)
    {
        return await ReadDBData(sql, "EDDS_CONNECTION_STRING");
    }
    
    
    
    public async Task<DbOperationResult> ReadDBData(string sql, string connectionStringName)
    {
        var conn = await _connectionFactory.GetOpenConnectionAsync(connectionStringName);
        try
        {
            using (conn)
            {
                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                var results = new List<Dictionary<string, object?>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);
                }
                return new DbOperationResult(success: true, data: results);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReadData failed: {Message}", ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
    }
}
