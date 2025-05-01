// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Data.SqlClient;

namespace Mssql.McpServer
{
    public static class SqlConnectionManager
    {
        public static string GetSqlConfig()
        {
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            return string.IsNullOrEmpty(connectionString)
                ? throw new InvalidOperationException("Connection string is not set in the environment variable 'CONNECTION_STRING'.\n\nHINT: Have a local SQL Server, with a database called 'test', from console, run `SET CONNECTION_STRING=Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True` and the load the .sln file")
                : connectionString;
        }

        public static async Task<SqlConnection> GetOpenConnectionAsync()
        {
            var connectionString = GetSqlConfig();

            // Let ADO.Net handing connection pooling, create a new connection per tool call
            var conn = new SqlConnection(connectionString);

            await conn.OpenAsync();
            return conn;
        }
    }
}
