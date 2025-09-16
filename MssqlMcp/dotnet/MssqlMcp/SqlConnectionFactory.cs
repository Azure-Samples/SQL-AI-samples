// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Data.SqlClient;

namespace Mssql.McpServer;

public class SqlConnectionFactory : ISqlConnectionFactory
{
    public async Task<SqlConnection> GetOpenConnectionAsync(string connectionStringName)
    {
        var connectionString = GetConnectionString(connectionStringName);

        // Let ADO.Net handle connection pooling
        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }

    private static string GetConnectionString(string connectionStringName)
    {
        var connectionString = Environment.GetEnvironmentVariable(connectionStringName);

        return string.IsNullOrEmpty(connectionString)
            ? throw new InvalidOperationException($"Connection string is not set in the environment variable '{connectionStringName}'.\n\nHINT: Have a local SQL Server, with a database called 'test', from console, run `SET {connectionStringName}=Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True` and the load the .sln file")
            : connectionString;
    }
}
