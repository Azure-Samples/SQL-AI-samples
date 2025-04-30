using System.Data;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mssql.McpServer;

public static partial class MssqlMcpTools
{
    const string ListTablesQuery = @"SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME";

    [McpServerTool, Description("Lists all tables in the SQL Database.")]
    public static async Task<object?> ListTables()
    {
        var (conn, error) = await SqlConnectionManager.GetOpenConnectionAsync();
        if (error != null) return error;
        try
        {
            using var cmd = new SqlCommand(ListTablesQuery, conn);
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
