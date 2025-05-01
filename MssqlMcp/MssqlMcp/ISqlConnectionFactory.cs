// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.Data.SqlClient;

namespace Mssql.McpServer;

/// <summary>
/// Defines a factory interface for creating SQL database connections.
/// </summary>
public interface ISqlConnectionFactory
{
    Task<SqlConnection> GetOpenConnectionAsync();
}