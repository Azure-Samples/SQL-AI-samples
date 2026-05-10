// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string DescribeViewQuery = @"
DECLARE @ObjectId INT = (
    SELECT v.object_id
    FROM sys.views v
    INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
    WHERE v.name = @ViewName AND (s.name = @ViewSchema OR @ViewSchema IS NULL)
);

-- Resultset 1: view info + definition
SELECT v.object_id AS id, v.name, s.name AS [schema], OBJECT_DEFINITION(v.object_id) AS definition,
       p.value AS description, v.create_date, v.modify_date
FROM sys.views v
INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
LEFT JOIN sys.extended_properties p ON p.major_id = v.object_id AND p.minor_id = 0 AND p.name = 'MS_Description'
WHERE v.object_id = @ObjectId;

-- Resultset 2: columns
SELECT c.name, ty.name AS type, c.max_length AS length, c.precision, c.scale, c.is_nullable AS nullable
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = @ObjectId
ORDER BY c.column_id;
";

    [McpServerTool(
        Title = "Describe View",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Returns a view's definition (T-SQL source) and its column list.")]
    public Task<DbOperationResult> DescribeView(
        [Description("View name (optionally schema-qualified, e.g. 'dbo.MyView')")] string name,
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
            using var cmd = CreateCommand(DescribeViewQuery, conn);
            cmd.Parameters.AddWithValue("@ViewName", name);
            cmd.Parameters.AddWithValue("@ViewSchema", (object?)schema ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            object? viewInfo = null;
            if (await reader.ReadAsync(ct))
            {
                viewInfo = new
                {
                    id = reader["id"],
                    name = reader["name"],
                    schema = reader["schema"],
                    definition = reader["definition"] is DBNull ? null : reader["definition"],
                    description = reader["description"] is DBNull ? null : reader["description"],
                    create_date = reader["create_date"],
                    modify_date = reader["modify_date"],
                };
            }

            if (viewInfo is null)
            {
                return DbOperationResult.Fail($"View '{name}' not found.");
            }

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
                });
            }

            return DbOperationResult.Ok(new { view = viewInfo, columns });
        }, database, cancellationToken);
    }
}
