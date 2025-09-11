// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Describe Workspace DB Table",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Returns table schema")]
    public async Task<DbOperationResult> DescribeWorkspaceDBTable(
        [Description("Name of table")] string name)
    {
        string? schema = null;
        if (name.Contains('.'))
        {
            // If the table name contains a schema, split it into schema and table name
            var parts = name.Split('.');
            if (parts.Length > 1)
            {
                name = parts[1]; // Use only the table name part
                schema = parts[0]; // Use the first part as schema  
            }
        }
        // Query for table metadata
        const string TableInfoQuery = @"SELECT t.object_id AS id, t.name, s.name AS [schema], p.value AS description, t.type, u.name AS owner
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            LEFT JOIN sys.extended_properties p ON p.major_id = t.object_id AND p.minor_id = 0 AND p.name = 'MS_Description'
            LEFT JOIN sys.sysusers u ON t.principal_id = u.uid
            WHERE t.name = @TableName and (s.name = @TableSchema or @TableSchema IS NULL) ";

        // Query for columns
        const string ColumnsQuery = @"SELECT c.name, ty.name AS type, c.max_length AS length, c.precision, c.scale, c.is_nullable AS nullable, p.value AS description
            FROM sys.columns c
            INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
            LEFT JOIN sys.extended_properties p ON p.major_id = c.object_id AND p.minor_id = c.column_id AND p.name = 'MS_Description'
            WHERE c.object_id = (SELECT object_id FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = @TableName and (s.name = @TableSchema or @TableSchema IS NULL ) )";

        // Query for indexes
        const string IndexesQuery = @"SELECT i.name, i.type_desc AS type, p.value AS description,
            STUFF((SELECT ',' + c.name FROM sys.index_columns ic
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 1, '') AS keys
            FROM sys.indexes i
            LEFT JOIN sys.extended_properties p ON p.major_id = i.object_id AND p.minor_id = i.index_id AND p.name = 'MS_Description'
            WHERE i.object_id = ( SELECT object_id FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = @TableName and (s.name = @TableSchema or @TableSchema IS NULL )  ) AND i.is_primary_key = 0 AND i.is_unique_constraint = 0";

        // Query for constraints
        const string ConstraintsQuery = @"SELECT kc.name, kc.type_desc AS type,
            STUFF((SELECT ',' + c.name FROM sys.index_columns ic
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 1, '') AS keys
            FROM sys.key_constraints kc
            WHERE kc.parent_object_id = (SELECT object_id FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = @TableName and (s.name = @TableSchema or @TableSchema IS NULL )  )";


        const string ForeignKeyInformation = @"SELECT
    fk.name AS name,
    SCHEMA_NAME(tp.schema_id) AS [schema],
    tp.name AS table_name,
    STRING_AGG(cp.name, ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS column_names,
    SCHEMA_NAME(tr.schema_id) AS referenced_schema,
    tr.name AS referenced_table,
    STRING_AGG(cr.name, ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS referenced_column_names
FROM
    sys.foreign_keys AS fk
JOIN
    sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
JOIN
    sys.tables AS tp ON fkc.parent_object_id = tp.object_id
JOIN
    sys.columns AS cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
JOIN
    sys.tables AS tr ON fkc.referenced_object_id = tr.object_id
JOIN
    sys.columns AS cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
 WHERE
            ( SCHEMA_NAME(tp.schema_id) = @TableSchema OR @TableSchema IS NULL )
            AND tp.name = @TableName
GROUP BY
    fk.name, tp.schema_id, tp.name, tr.schema_id, tr.name;
";
        var conn = await _connectionFactory.GetOpenConnectionAsync();
        try
        {
            using (conn)
            {
                var result = new Dictionary<string, object>();
                // Table info
                using (var cmd = new SqlCommand(TableInfoQuery, conn))
                {
                    var _ = cmd.Parameters.AddWithValue("@TableName", name);
                    _ = cmd.Parameters.AddWithValue("@TableSchema", schema == null ? DBNull.Value : schema);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        result["table"] = new
                        {
                            id = reader["id"],
                            name = reader["name"],
                            schema = reader["schema"],
                            owner = reader["owner"],
                            type = reader["type"],
                            description = reader["description"] is DBNull ? null : reader["description"]
                        };
                    }
                    else
                    {
                        return new DbOperationResult(success: false, error: $"Table '{name}' not found.");
                    }
                }
                // Columns
                using (var cmd = new SqlCommand(ColumnsQuery, conn))
                {
                    var _ = cmd.Parameters.AddWithValue("@TableName", name);
                    _ = cmd.Parameters.AddWithValue("@TableSchema", schema == null ? DBNull.Value : schema);
                    using var reader = await cmd.ExecuteReaderAsync();
                    var columns = new List<object>();
                    while (await reader.ReadAsync())
                    {
                        columns.Add(new
                        {
                            name = reader["name"],
                            type = reader["type"],
                            length = reader["length"],
                            precision = reader["precision"],
                            scale = reader["scale"],
                            nullable = (bool)reader["nullable"],
                            description = reader["description"] is DBNull ? null : reader["description"]
                        });
                    }
                    result["columns"] = columns;
                }
                // Indexes
                using (var cmd = new SqlCommand(IndexesQuery, conn))
                {
                    var _ = cmd.Parameters.AddWithValue("@TableName", name);
                    _ = cmd.Parameters.AddWithValue("@TableSchema", schema == null ? DBNull.Value : schema);
                    using var reader = await cmd.ExecuteReaderAsync();
                    var indexes = new List<object>();
                    while (await reader.ReadAsync())
                    {
                        indexes.Add(new
                        {
                            name = reader["name"],
                            type = reader["type"],
                            description = reader["description"] is DBNull ? null : reader["description"],
                            keys = reader["keys"]
                        });
                    }
                    result["indexes"] = indexes;
                }
                // Constraints
                using (var cmd = new SqlCommand(ConstraintsQuery, conn))
                {
                    var _ = cmd.Parameters.AddWithValue("@TableName", name);
                    _ = cmd.Parameters.AddWithValue("@TableSchema", schema == null ? DBNull.Value : schema);
                    using var reader = await cmd.ExecuteReaderAsync();
                    var constraints = new List<object>();
                    while (await reader.ReadAsync())
                    {
                        constraints.Add(new
                        {
                            name = reader["name"],
                            type = reader["type"],
                            keys = reader["keys"]
                        });
                    }
                    result["constraints"] = constraints;
                }

                // Foreign Keys
                using (var cmd = new SqlCommand(ForeignKeyInformation, conn))
                {
                    var _ = cmd.Parameters.AddWithValue("@TableName", name);
                    _ = cmd.Parameters.AddWithValue("@TableSchema", schema == null ? DBNull.Value : schema);
                    using var reader = await cmd.ExecuteReaderAsync();
                    var foreignKeys = new List<object>();
                    while (await reader.ReadAsync())
                    {
                        foreignKeys.Add(new
                        {
                            name = reader["name"],
                            schema = reader["schema"],
                            table_name = reader["table_name"],
                            column_name = reader["column_names"],
                            referenced_schema = reader["referenced_schema"],
                            referenced_table = reader["referenced_table"],
                            referenced_column = reader["referenced_column_names"],
                        });
                    }
                    result["foreignKeys"] = foreignKeys;
                }

                return new DbOperationResult(success: true, data: result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DescribeTable failed: {Message}", ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
    }
}
