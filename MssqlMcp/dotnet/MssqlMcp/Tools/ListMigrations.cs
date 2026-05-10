// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private const string CheckMigrationsTableQuery = @"SELECT 1 FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = '__EFMigrationsHistory' AND s.name = 'dbo'";
    private const string ReadMigrationsQuery = @"SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId";

    [McpServerTool(
        Title = "List Migrations",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Reads EF Core's __EFMigrationsHistory table and returns the list of applied migrations (id + EF Core version). Returns empty list and a flag if the table doesn't exist.")]
    public Task<DbOperationResult> ListMigrations(
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            using (var checkCmd = CreateCommand(CheckMigrationsTableQuery, conn))
            {
                var exists = await checkCmd.ExecuteScalarAsync(ct);
                if (exists is null)
                {
                    return DbOperationResult.Ok(new { table_exists = false, migrations = Array.Empty<object>() });
                }
            }

            using var cmd = CreateCommand(ReadMigrationsQuery, conn);
            using var reader = await cmd.ExecuteReaderAsync(ct);
            var migrations = new List<object>();
            while (await reader.ReadAsync(ct))
            {
                migrations.Add(new
                {
                    migration_id = reader.GetString(0),
                    product_version = reader.GetString(1),
                });
            }
            return DbOperationResult.Ok(new { table_exists = true, migrations });
        }, database, cancellationToken);
    }
}
