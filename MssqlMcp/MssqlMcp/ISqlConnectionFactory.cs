// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Data.SqlClient;

namespace Mssql.McpServer;

public interface ISqlConnectionFactory
{
    Task<SqlConnection> GetOpenConnectionAsync();
}