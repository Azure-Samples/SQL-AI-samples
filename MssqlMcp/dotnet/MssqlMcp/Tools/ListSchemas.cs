// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string ListSchemasQuery = @"SELECT name
        FROM sys.schemas
        WHERE name NOT IN ('sys','INFORMATION_SCHEMA','db_owner','db_accessadmin','db_securityadmin','db_ddladmin',
                           'db_backupoperator','db_datareader','db_datawriter','db_denydatareader','db_denydatawriter','guest')
        ORDER BY name";

    [McpServerTool(
        Title = "List Schemas",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Lists user-visible schemas in the SQL Database (excludes system and fixed-role schemas).")]
    public Task<DbOperationResult> ListSchemas(
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            using var cmd = CreateCommand(ListSchemasQuery, conn);
            using var reader = await cmd.ExecuteReaderAsync(ct);
            var schemas = new List<string>();
            while (await reader.ReadAsync(ct))
            {
                schemas.Add(reader.GetString(0));
            }
            return DbOperationResult.Ok(schemas);
        }, database, cancellationToken);
    }
}
