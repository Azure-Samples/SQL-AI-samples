// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Data.SqlClient;

namespace Mssql.McpServer;

/// <summary>
/// Defines a factory interface for creating SQL database connections.
/// </summary>
public interface ISqlConnectionFactory
{
    /// <summary>
    /// Opens a connection to the configured server. When <paramref name="database"/> is provided
    /// it overrides the <c>Initial Catalog</c> from the connection string, enabling multi-database scenarios.
    /// </summary>
    Task<SqlConnection> GetOpenConnectionAsync(string? database = null, CancellationToken cancellationToken = default);
}
