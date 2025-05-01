// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Data.SqlClient;

namespace Mssql.McpServer;

public class SqlConnectionFactory : ISqlConnectionFactory
{
    public async Task<SqlConnection> GetOpenConnectionAsync()
    {
        var connectionString = SqlConnectionManager.GetSqlConfig();
        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }
}
