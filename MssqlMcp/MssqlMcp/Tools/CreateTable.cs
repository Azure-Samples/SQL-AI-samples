// Copyright (c) Microsoft Corporation. All rights reserved.

using System.ComponentModel;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool, Description("Creates a new table in the SQL Database. Expects a valid CREATE TABLE SQL statement as input.")]
    public async Task<object?> CreateTable(
        [Description("CREATE TABLE SQL statement")] string sql)
    {
        var conn = await _connectionFactory.GetOpenConnectionAsync();
        try
        {
            using (conn)
            {
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                _ = await cmd.ExecuteNonQueryAsync();
                return new DbOperationResult { Success = true };
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CreateTable failed: {Message}", ex.Message);
            return new DbOperationResult { Success = false, Error = ex.Message };
        }
    }
}
