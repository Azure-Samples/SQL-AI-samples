// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Mssql.McpServer;

public interface ISqlConnectionFactory
{
    Task<Microsoft.Data.SqlClient.SqlConnection> GetOpenConnectionAsync();
}

