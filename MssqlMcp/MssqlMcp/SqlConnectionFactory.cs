// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Mssql.McpServer;

public class SqlConnectionFactory : ISqlConnectionFactory
{
    public async Task<Microsoft.Data.SqlClient.SqlConnection> GetOpenConnectionAsync()
    {
        var connectionString = SqlConnectionManager.GetSqlConfig();
        var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }
}
