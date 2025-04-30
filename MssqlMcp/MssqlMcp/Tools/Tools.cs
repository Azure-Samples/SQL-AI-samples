using System.Data;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

// Register this class as a tool container
[McpServerToolType]
public static partial class MssqlMcpTools
{
    // Helper to convert DataTable to a serializable list
    private static List<Dictionary<string, object?>> DataTableToList(DataTable table)
    {
        var result = new List<Dictionary<string, object?>>();
        foreach (DataRow row in table.Rows)
        {
            var dict = new Dictionary<string, object?>();
            foreach (DataColumn col in table.Columns)
                dict[col.ColumnName] = row[col];
            result.Add(dict);
        }
        return result;
    }
}
