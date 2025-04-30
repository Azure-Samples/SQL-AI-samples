using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mssql.McpServer;

public static partial class MssqlMcpTools
{
    [McpServerTool, Description("Updates data in a table in the SQL Database. Expects a valid INSERT SQL statement as input. ")]
    public static async Task<object?> InsertData(
        [Description("INSERT SQL statement")] string sql)
    {
        var (conn, error) = await SqlConnectionManager.GetOpenConnectionAsync();
        if (error != null) return error;
        try
        {
            using var cmd = new SqlCommand(sql, conn);
            var rows = await cmd.ExecuteNonQueryAsync();
            return new { success = true, rowsAffected = rows };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }
}
