// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string DescribeTableMultiQuery = @"
DECLARE @ObjectId INT = (
    SELECT t.object_id
    FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = @TableName AND (s.name = @TableSchema OR @TableSchema IS NULL)
);

-- Resultset 1: table info
SELECT t.object_id AS id, t.name, s.name AS [schema], p.value AS description, t.type, u.name AS owner
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
LEFT JOIN sys.extended_properties p ON p.major_id = t.object_id AND p.minor_id = 0 AND p.name = 'MS_Description'
LEFT JOIN sys.sysusers u ON t.principal_id = u.uid
WHERE t.object_id = @ObjectId;

-- Resultset 2: columns (with identity, computed, default, description)
SELECT
    c.name,
    ty.name AS type,
    c.max_length AS length,
    c.precision,
    c.scale,
    c.is_nullable AS nullable,
    c.is_identity AS is_identity,
    c.is_computed AS is_computed,
    cc.definition AS computed_definition,
    dc.definition AS default_definition,
    p.value AS description
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
LEFT JOIN sys.default_constraints dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
LEFT JOIN sys.extended_properties p ON p.major_id = c.object_id AND p.minor_id = c.column_id AND p.name = 'MS_Description'
WHERE c.object_id = @ObjectId
ORDER BY c.column_id;

-- Resultset 3: indexes (non-PK / non-UQ)
SELECT i.name, i.type_desc AS type, i.is_unique AS is_unique, p.value AS description,
    STUFF((SELECT ',' + c.name FROM sys.index_columns ic
        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 1, '') AS keys
FROM sys.indexes i
LEFT JOIN sys.extended_properties p ON p.major_id = i.object_id AND p.minor_id = i.index_id AND p.name = 'MS_Description'
WHERE i.object_id = @ObjectId AND i.is_primary_key = 0 AND i.is_unique_constraint = 0 AND i.type > 0;

-- Resultset 4: key constraints (PK / UQ)
SELECT kc.name, kc.type_desc AS type,
    STUFF((SELECT ',' + c.name FROM sys.index_columns ic
        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 1, '') AS keys
FROM sys.key_constraints kc
WHERE kc.parent_object_id = @ObjectId;

-- Resultset 5: check constraints
SELECT cc.name, cc.definition, cc.is_disabled, cc.is_not_trusted
FROM sys.check_constraints cc
WHERE cc.parent_object_id = @ObjectId;

-- Resultset 6: outbound foreign keys
SELECT
    fk.name AS name,
    SCHEMA_NAME(tp.schema_id) AS [schema],
    tp.name AS table_name,
    STRING_AGG(cp.name, ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS column_names,
    SCHEMA_NAME(tr.schema_id) AS referenced_schema,
    tr.name AS referenced_table,
    STRING_AGG(cr.name, ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS referenced_column_names,
    fk.delete_referential_action_desc AS on_delete,
    fk.update_referential_action_desc AS on_update
FROM sys.foreign_keys AS fk
JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.tables AS tp ON fkc.parent_object_id = tp.object_id
JOIN sys.columns AS cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
JOIN sys.tables AS tr ON fkc.referenced_object_id = tr.object_id
JOIN sys.columns AS cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
WHERE tp.object_id = @ObjectId
GROUP BY fk.name, tp.schema_id, tp.name, tr.schema_id, tr.name, fk.delete_referential_action_desc, fk.update_referential_action_desc;

-- Resultset 7: inbound foreign keys (other tables referencing this one)
SELECT
    fk.name AS name,
    SCHEMA_NAME(tp.schema_id) AS [schema],
    tp.name AS table_name,
    STRING_AGG(cp.name, ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS column_names,
    SCHEMA_NAME(tr.schema_id) AS referenced_schema,
    tr.name AS referenced_table,
    STRING_AGG(cr.name, ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS referenced_column_names,
    fk.delete_referential_action_desc AS on_delete,
    fk.update_referential_action_desc AS on_update
FROM sys.foreign_keys AS fk
JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.tables AS tp ON fkc.parent_object_id = tp.object_id
JOIN sys.columns AS cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
JOIN sys.tables AS tr ON fkc.referenced_object_id = tr.object_id
JOIN sys.columns AS cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
WHERE tr.object_id = @ObjectId
GROUP BY fk.name, tp.schema_id, tp.name, tr.schema_id, tr.name, fk.delete_referential_action_desc, fk.update_referential_action_desc;

-- Resultset 8: triggers
SELECT
    tr.name,
    tr.is_disabled,
    tr.is_instead_of_trigger,
    STUFF((SELECT ',' + te.type_desc FROM sys.trigger_events te WHERE te.object_id = tr.object_id FOR XML PATH('')), 1, 1, '') AS events
FROM sys.triggers tr
WHERE tr.parent_id = @ObjectId AND tr.is_ms_shipped = 0;
";

    [McpServerTool(
        Title = "Describe Table",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Returns full table schema (columns with identity/computed/default, indexes, PK/UQ constraints, check constraints, outbound + inbound foreign keys, triggers) in a single round-trip.")]
    public Task<DbOperationResult> DescribeTable(
        [Description("Name of table (optionally schema-qualified, e.g. 'dbo.Users')")] string name,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        string? schema = null;
        if (name.Contains('.'))
        {
            var parts = name.Split('.', 2);
            if (parts.Length == 2)
            {
                schema = parts[0];
                name = parts[1];
            }
        }

        var deny = CheckSchemaAllowed(schema);
        if (deny is not null) return Task.FromResult(deny);

        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(DescribeTableMultiQuery, conn);
            cmd.Parameters.AddWithValue("@TableName", name);
            cmd.Parameters.AddWithValue("@TableSchema", (object?)schema ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            var result = new Dictionary<string, object?>();

            // 1: table info
            object? tableInfo = null;
            if (await reader.ReadAsync(ct))
            {
                tableInfo = new
                {
                    id = reader["id"],
                    name = reader["name"],
                    schema = reader["schema"],
                    owner = reader["owner"] is DBNull ? null : reader["owner"],
                    type = reader["type"],
                    description = reader["description"] is DBNull ? null : reader["description"],
                };
            }

            if (tableInfo is null)
            {
                return DbOperationResult.Fail($"Table '{name}' not found.");
            }
            result["table"] = tableInfo;

            // 2: columns
            await reader.NextResultAsync(ct);
            var columns = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                columns.Add(new
                {
                    name = reader["name"],
                    type = reader["type"],
                    length = reader["length"],
                    precision = reader["precision"],
                    scale = reader["scale"],
                    nullable = (bool)reader["nullable"],
                    is_identity = (bool)reader["is_identity"],
                    is_computed = (bool)reader["is_computed"],
                    computed_definition = reader["computed_definition"] is DBNull ? null : reader["computed_definition"],
                    default_definition = reader["default_definition"] is DBNull ? null : reader["default_definition"],
                    description = reader["description"] is DBNull ? null : reader["description"],
                });
            }
            result["columns"] = columns;

            // 3: indexes
            await reader.NextResultAsync(ct);
            var indexes = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                indexes.Add(new
                {
                    name = reader["name"],
                    type = reader["type"],
                    is_unique = (bool)reader["is_unique"],
                    description = reader["description"] is DBNull ? null : reader["description"],
                    keys = reader["keys"],
                });
            }
            result["indexes"] = indexes;

            // 4: key constraints
            await reader.NextResultAsync(ct);
            var constraints = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                constraints.Add(new
                {
                    name = reader["name"],
                    type = reader["type"],
                    keys = reader["keys"],
                });
            }
            result["constraints"] = constraints;

            // 5: check constraints
            await reader.NextResultAsync(ct);
            var checks = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                checks.Add(new
                {
                    name = reader["name"],
                    definition = reader["definition"],
                    is_disabled = (bool)reader["is_disabled"],
                    is_not_trusted = (bool)reader["is_not_trusted"],
                });
            }
            result["checkConstraints"] = checks;

            // 6: foreign keys outbound
            await reader.NextResultAsync(ct);
            var foreignKeys = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                foreignKeys.Add(BuildForeignKey(reader));
            }
            result["foreignKeys"] = foreignKeys;

            // 7: foreign keys inbound
            await reader.NextResultAsync(ct);
            var referencedBy = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                referencedBy.Add(BuildForeignKey(reader));
            }
            result["referencedBy"] = referencedBy;

            // 8: triggers
            await reader.NextResultAsync(ct);
            var triggers = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                triggers.Add(new
                {
                    name = reader["name"],
                    is_disabled = (bool)reader["is_disabled"],
                    is_instead_of = (bool)reader["is_instead_of_trigger"],
                    events = reader["events"] is DBNull ? null : reader["events"],
                });
            }
            result["triggers"] = triggers;

            return DbOperationResult.Ok(result);
        }, database, cancellationToken);
    }

    private static object BuildForeignKey(Microsoft.Data.SqlClient.SqlDataReader reader) => new
    {
        name = reader["name"],
        schema = reader["schema"],
        table_name = reader["table_name"],
        column_name = reader["column_names"],
        referenced_schema = reader["referenced_schema"],
        referenced_table = reader["referenced_table"],
        referenced_column = reader["referenced_column_names"],
        on_delete = reader["on_delete"],
        on_update = reader["on_update"],
    };

    private static void AddSchemaAndTable(Microsoft.Data.SqlClient.SqlCommand cmd, string? schema, string name)
    {
        cmd.Parameters.AddWithValue("@TableName", name);
        cmd.Parameters.AddWithValue("@TableSchema", schema is null ? DBNull.Value : schema);
    }
}
