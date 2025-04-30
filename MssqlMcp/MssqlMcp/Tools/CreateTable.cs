using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mssql.McpServer;

public static partial class MssqlMcpTools
{
    [McpServerTool, Description("Creates a new table in the SQL Database. Expects a valid CREATE TABLE SQL statement as input.")]
    public static async Task<object?> CreateTable(
        [Description("CREATE TABLE SQL statement")] string sql)
    {
        var (conn, error) = await SqlConnectionManager.GetOpenConnectionAsync();
        if (error != null) return error;
        try
        {
            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
            return new { success = true };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }
}
