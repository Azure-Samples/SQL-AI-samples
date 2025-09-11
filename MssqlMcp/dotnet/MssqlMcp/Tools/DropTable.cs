// // Copyright (c) Microsoft Corporation. All rights reserved.
// // Licensed under the MIT license.

// using System.ComponentModel;
// using Microsoft.Extensions.Logging;
// using ModelContextProtocol.Server;

// namespace Mssql.McpServer;

// public partial class Tools
// {
//     [McpServerTool(
//         Title = "Drop Table",
//         ReadOnly = false,
//         Destructive = true),
//         Description("Drops a table in the SQL Database. Expects a valid DROP TABLE SQL statement as input.")]
//     public async Task<DbOperationResult> DropTable(
//         [Description("DROP TABLE SQL statement")] string sql)
//     {
//         var conn = await _connectionFactory.GetOpenConnectionAsync();
//         try
//         {
//             using (conn)
//             {
//                 using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
//                 _ = await cmd.ExecuteNonQueryAsync();
//                 return new DbOperationResult(success: true);
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "DropTable failed: {Message}", ex.Message);
//             return new DbOperationResult(success: false, error: ex.Message);
//         }
//     }
// }
