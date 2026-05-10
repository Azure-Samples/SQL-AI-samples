// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string DescribeProcedureQuery = @"
DECLARE @ObjectId INT = (
    SELECT p.object_id
    FROM sys.procedures p
    INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
    WHERE p.name = @Name AND (s.name = @Schema OR @Schema IS NULL)
);

-- Resultset 1: procedure info + definition
SELECT p.object_id AS id, p.name, s.name AS [schema],
       OBJECT_DEFINITION(p.object_id) AS definition,
       p.create_date, p.modify_date,
       ep.value AS description
FROM sys.procedures p
INNER JOIN sys.schemas s ON p.schema_id = s.schema_id
LEFT JOIN sys.extended_properties ep ON ep.major_id = p.object_id AND ep.minor_id = 0 AND ep.name = 'MS_Description'
WHERE p.object_id = @ObjectId;

-- Resultset 2: parameters
SELECT pa.name, ty.name AS type, pa.max_length, pa.precision, pa.scale,
       pa.is_output, pa.has_default_value, pa.default_value, pa.parameter_id
FROM sys.parameters pa
INNER JOIN sys.types ty ON pa.user_type_id = ty.user_type_id
WHERE pa.object_id = @ObjectId
ORDER BY pa.parameter_id;
";

    [McpServerTool(
        Title = "Describe Procedure",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Returns a stored procedure's definition (T-SQL source) and parameter list.")]
    public Task<DbOperationResult> DescribeProcedure(
        [Description("Procedure name (optionally schema-qualified)")] string name,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        string? schema = null;
        if (name.Contains('.'))
        {
            var parts = name.Split('.', 2);
            schema = parts[0];
            name = parts[1];
        }

        var deny = CheckSchemaAllowed(schema);
        if (deny is not null) return Task.FromResult(deny);

        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(DescribeProcedureQuery, conn);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Schema", (object?)schema ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            object? procInfo = null;
            if (await reader.ReadAsync(ct))
            {
                procInfo = new
                {
                    id = reader["id"],
                    name = reader["name"],
                    schema = reader["schema"],
                    definition = reader["definition"] is DBNull ? null : reader["definition"],
                    create_date = reader["create_date"],
                    modify_date = reader["modify_date"],
                    description = reader["description"] is DBNull ? null : reader["description"],
                };
            }

            if (procInfo is null)
            {
                return DbOperationResult.Fail($"Procedure '{name}' not found.");
            }

            await reader.NextResultAsync(ct);
            var parameters = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                parameters.Add(new
                {
                    name = reader["name"],
                    type = reader["type"],
                    max_length = reader["max_length"],
                    precision = reader["precision"],
                    scale = reader["scale"],
                    is_output = (bool)reader["is_output"],
                    has_default_value = (bool)reader["has_default_value"],
                    default_value = reader["default_value"] is DBNull ? null : reader["default_value"],
                    ordinal = reader["parameter_id"],
                });
            }

            return DbOperationResult.Ok(new { procedure = procInfo, parameters });
        }, database, cancellationToken);
    }
}
