// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string ListProceduresAndFunctionsQuery = @"
        SELECT 
            SCHEMA_NAME(schema_id) AS [Schema],
            name AS [Name],
            type_desc AS [Type],
            create_date AS [Created],
            modify_date AS [Modified]
        FROM sys.objects 
        WHERE type IN ('P', 'FN', 'IF', 'TF', 'PC', 'FS', 'FT')
        ORDER BY SCHEMA_NAME(schema_id), type_desc, name";

    [McpServerTool(
        Title = "List Procedures and Functions",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists all stored procedures and functions in the SQL Database.")]
    public async Task<DbOperationResult> ListProceduresAndFunctions()
    {
        var conn = await _connectionFactory.GetOpenConnectionAsync();
        try
        {
            using (conn)
            {
                using var cmd = new SqlCommand(ListProceduresAndFunctionsQuery, conn);
                var proceduresAndFunctions = new List<object>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    proceduresAndFunctions.Add(new
                    {
                        Schema = reader.GetString(0),
                        Name = reader.GetString(1),
                        Type = reader.GetString(2),
                        Created = reader.GetDateTime(3),
                        Modified = reader.GetDateTime(4),
                        FullName = $"{reader.GetString(0)}.{reader.GetString(1)}"
                    });
                }
                return new DbOperationResult(success: true, data: proceduresAndFunctions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListProceduresAndFunctions failed: {Message}", ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
    }
}
