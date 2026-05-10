// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string DependenciesIncomingQuery = @"
SELECT DISTINCT
    OBJECT_SCHEMA_NAME(d.referencing_id) AS [schema],
    OBJECT_NAME(d.referencing_id) AS name,
    o.type_desc AS object_type
FROM sys.sql_expression_dependencies d
INNER JOIN sys.objects o ON o.object_id = d.referencing_id
WHERE d.referenced_id = OBJECT_ID(@FullName)
ORDER BY object_type, [schema], name;";

    private const string DependenciesOutgoingQuery = @"
SELECT DISTINCT
    COALESCE(d.referenced_schema_name, OBJECT_SCHEMA_NAME(d.referenced_id)) AS [schema],
    d.referenced_entity_name AS name,
    o.type_desc AS object_type
FROM sys.sql_expression_dependencies d
LEFT JOIN sys.objects o ON o.object_id = d.referenced_id
WHERE d.referencing_id = OBJECT_ID(@FullName)
ORDER BY object_type, [schema], name;";

    [McpServerTool(
        Title = "Get Table Dependencies",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Returns objects that reference a given table/view/procedure (incoming) and objects it references (outgoing). Useful before renaming or dropping.")]
    public Task<DbOperationResult> GetTableDependencies(
        [Description("Object name, optionally schema-qualified, e.g. 'dbo.Users'")] string name,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        var fullName = name.Contains('.') ? name : $"dbo.{name}";

        return ExecuteAsync(async (conn, ct) =>
        {
            var incoming = new List<object>();
            using (var cmd = CreateCommand(DependenciesIncomingQuery, conn))
            {
                cmd.Parameters.AddWithValue("@FullName", fullName);
                using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    incoming.Add(new
                    {
                        schema = reader["schema"],
                        name = reader["name"],
                        object_type = reader["object_type"],
                    });
                }
            }

            var outgoing = new List<object>();
            using (var cmd = CreateCommand(DependenciesOutgoingQuery, conn))
            {
                cmd.Parameters.AddWithValue("@FullName", fullName);
                using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    outgoing.Add(new
                    {
                        schema = reader["schema"] is DBNull ? null : reader["schema"],
                        name = reader["name"],
                        object_type = reader["object_type"] is DBNull ? null : reader["object_type"],
                    });
                }
            }

            return DbOperationResult.Ok(new { incoming, outgoing });
        }, database, cancellationToken);
    }
}
