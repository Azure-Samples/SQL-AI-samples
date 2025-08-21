// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Execute Function",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Executes a SQL function (table-valued or scalar) in the SQL Database with optional parameters. Returns the function's result set.")]
    public async Task<DbOperationResult> ExecuteFunction(
        [Description("Name of the function to execute")] string functionName,
        [Description("Optional parameters for the function as key-value pairs")] Dictionary<string, object>? parameters = null)
    {
        try
        {
            using var connection = await _connectionFactory.GetOpenConnectionAsync();
            
            // Build the function call SQL
            var paramString = parameters != null && parameters.Any() 
                ? string.Join(", ", parameters.Keys.Select(k => $"@{k}"))
                : "";
            
            var sql = $"SELECT * FROM {functionName}({paramString})";
            
            using var command = new SqlCommand(sql, connection);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                }
            }

            using var reader = await command.ExecuteReaderAsync();
            var dataTable = new DataTable();
            dataTable.Load(reader);
            
            var results = DataTableToList(dataTable);

            return new DbOperationResult(
                success: true,
                rowsAffected: results.Count,
                data: results
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return new DbOperationResult(
                success: false,
                error: $"Error executing function: {ex.Message}"
            );
        }
    }
}
