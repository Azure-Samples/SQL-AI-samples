// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Create Procedure",
        ReadOnly = false,
        Destructive = false),
        Description("Creates a new stored procedure in the SQL Database. Expects a valid CREATE PROCEDURE SQL statement as input. Use CREATE OR ALTER to update existing procedures.")]
    public async Task<DbOperationResult> CreateProcedure(
        [Description("CREATE PROCEDURE SQL statement")] string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return new DbOperationResult(success: false, error: "SQL statement is required");
        }

        // Basic validation to ensure it's a procedure creation statement
        var trimmedSql = sql.Trim();
        if (!trimmedSql.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase) || 
            !trimmedSql.Contains("PROCEDURE", StringComparison.OrdinalIgnoreCase))
        {
            return new DbOperationResult(success: false, error: "SQL statement must be a CREATE PROCEDURE statement");
        }

        var conn = await _connectionFactory.GetOpenConnectionAsync();
        try
        {
            using (conn)
            {
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
                _ = await cmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("Successfully created stored procedure");
               return new DbOperationResult(success: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateProcedure failed: {Message}", ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
    }
}