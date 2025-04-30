using System.Data;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mssql.McpServer;
public static partial class MssqlMcpTools
{
    [McpServerTool, Description("Executes SQL queries against SQL Database to read data")]
    public static async Task<object?> ReadData(
        [Description("SQL query to execute")] string sql)
    {
        var (conn, error) = await SqlConnectionManager.GetOpenConnectionAsync();
        if (error != null) return error;
        try
        {
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            var table = new DataTable();
            table.Load(reader);
            return DataTableToList(table);
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }
}
