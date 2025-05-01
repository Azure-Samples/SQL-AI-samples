// Copyright (c) Microsoft Corporation. All rights reserved.

using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace Mssql.McpServer;
public partial class Tools
{
    [McpServerTool, Description("Executes SQL queries against SQL Database to read data")]
    public async Task<DbOperationResult> ReadData(
        [Description("SQL query to execute")] string sql)
    {
        var conn = await _connectionFactory.GetOpenConnectionAsync();
        try
        {
            using (conn)
            {
                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                var table = new DataTable();
                table.Load(reader);
                return new DbOperationResult { Success = true, Data = DataTableToList(table) };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReadData failed: {Message}", ex.Message);
            return new DbOperationResult { Success = false, Error = ex.Message };
        }
    }
}
