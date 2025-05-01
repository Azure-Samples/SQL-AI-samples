// Copyright (c) Microsoft Corporation. All rights reserved.

using System.ComponentModel;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool, Description("Updates data in a table in the SQL Database. Expects a valid INSERT SQL statement as input. ")]
    public async Task<object?> InsertData(
        [Description("INSERT SQL statement")] string sql)
    {
        var conn = await _connectionFactory.GetOpenConnectionAsync();
        try
        {
            using (conn)
            {
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                var rows = await cmd.ExecuteNonQueryAsync();
                return new DbOperationResult { Success = true, RowsAffected = rows };
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "InsertData failed: {Message}", ex.Message);
            return new DbOperationResult { Success = false, Error = ex.Message };
        }
    }
}
