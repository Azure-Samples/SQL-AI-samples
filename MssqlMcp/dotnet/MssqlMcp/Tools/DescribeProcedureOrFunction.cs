// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string DescribeProcedureOrFunctionQuery = @"
        SELECT 
            SCHEMA_NAME(o.schema_id) AS [Schema],
            o.name AS [Name],
            o.type_desc AS [Type],
            o.create_date AS [Created],
            o.modify_date AS [Modified],
            m.definition AS [Definition]
        FROM sys.objects o
        LEFT JOIN sys.sql_modules m ON o.object_id = m.object_id
        WHERE o.type IN ('P', 'FN', 'IF', 'TF', 'PC', 'FS', 'FT')
        AND SCHEMA_NAME(o.schema_id) = @SchemaName 
        AND o.name = @ObjectName";

    private const string GetParametersQuery = @"
        SELECT 
            p.name AS [ParameterName],
            TYPE_NAME(p.user_type_id) AS [DataType],
            p.max_length,
            p.precision,
            p.scale,
            p.is_output AS [IsOutput],
            p.has_default_value AS [HasDefault],
            p.default_value AS [DefaultValue]
        FROM sys.parameters p
        INNER JOIN sys.objects o ON p.object_id = o.object_id
        WHERE SCHEMA_NAME(o.schema_id) = @SchemaName 
        AND o.name = @ObjectName
        ORDER BY p.parameter_id";

    [McpServerTool(
        Title = "Describe Procedure or Function",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Describes a stored procedure or function including its definition and parameters.")]
    public async Task<DbOperationResult> DescribeProcedureOrFunction(
        [Description("Schema name")] string schemaName,
        [Description("Procedure or function name")] string objectName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            return new DbOperationResult(success: false, error: "Schema name is required");
        }

        if (string.IsNullOrWhiteSpace(objectName))
        {
            return new DbOperationResult(success: false, error: "Object name is required");
        }

        var conn = await _connectionFactory.GetOpenConnectionAsync();
        try
        {
            using (conn)
            {
                // Get the object details
                using var cmd1 = new SqlCommand(DescribeProcedureOrFunctionQuery, conn);
                cmd1.Parameters.AddWithValue("@SchemaName", schemaName);
                cmd1.Parameters.AddWithValue("@ObjectName", objectName);

                object? objectDetails = null;
                using var reader1 = await cmd1.ExecuteReaderAsync();
                if (await reader1.ReadAsync())
                {
                    objectDetails = new
                    {
                        Schema = reader1.GetString(0),
                        Name = reader1.GetString(1),
                        Type = reader1.GetString(2),
                        Created = reader1.GetDateTime(3),
                        Modified = reader1.GetDateTime(4),
                        Definition = reader1.IsDBNull(5) ? null : reader1.GetString(5)
                    };
                }
                reader1.Close();

                if (objectDetails == null)
                {
                    return new DbOperationResult(success: false, error: $"Procedure or function '{schemaName}.{objectName}' not found");
                }

                // Get the parameters
                using var cmd2 = new SqlCommand(GetParametersQuery, conn);
                cmd2.Parameters.AddWithValue("@SchemaName", schemaName);
                cmd2.Parameters.AddWithValue("@ObjectName", objectName);

                var parameters = new List<object>();
                using var reader2 = await cmd2.ExecuteReaderAsync();
                while (await reader2.ReadAsync())
                {
                    parameters.Add(new
                    {
                        Name = reader2.IsDBNull(0) ? null : reader2.GetString(0),
                        DataType = reader2.GetString(1),
                        MaxLength = reader2.GetInt16(2),
                        Precision = reader2.GetByte(3),
                        Scale = reader2.GetByte(4),
                        IsOutput = reader2.GetBoolean(5),
                        HasDefault = reader2.GetBoolean(6),
                        DefaultValue = reader2.IsDBNull(7) ? null : reader2.GetString(7)
                    });
                }

                var result = new
                {
                    ObjectDetails = objectDetails,
                    Parameters = parameters
                };

                return new DbOperationResult(success: true, data: result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DescribeProcedureOrFunction failed for {Schema}.{Object}: {Message}", 
                schemaName, objectName, ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
    }
}
