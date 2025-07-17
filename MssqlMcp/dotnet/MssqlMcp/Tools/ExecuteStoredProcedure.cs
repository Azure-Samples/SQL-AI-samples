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
        Title = "Execute Stored Procedure",
        ReadOnly = false,
        Destructive = false),
        Description("Executes a stored procedure in the SQL Database with optional parameters. Can return result sets or scalar values.")]
    public async Task<DbOperationResult> ExecuteStoredProcedure(
        [Description("Name of the stored procedure to execute")] string procedureName,
        [Description("Optional parameters for the stored procedure as key-value pairs")] Dictionary<string, object>? parameters = null)
    {
        try
        {
            using var connection = await _connectionFactory.GetOpenConnectionAsync();
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            using var reader = await command.ExecuteReaderAsync();
            var results = new List<Dictionary<string, object>>();
            var dataTable = new DataTable();
            dataTable.Load(reader);
            
            results = DataTableToList(dataTable);

            return new DbOperationResult(
                success: true,
                rowsAffected: results.Count,
                data: results
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {ProcedureName}", procedureName);
            return new DbOperationResult(
                success: false,
                error: $"Error executing stored procedure: {ex.Message}"
            );
        }
    }
}
