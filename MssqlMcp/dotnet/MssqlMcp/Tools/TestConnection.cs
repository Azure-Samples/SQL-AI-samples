// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Test Connection",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Tests the database connection and returns connection status and basic server information.")]
    public async Task<DbOperationResult> TestConnection()
    {
        try
        {
            var conn = await _connectionFactory.GetOpenConnectionAsync();
            using (conn)
            {
                var connectionInfo = new Dictionary<string, object>
                {
                    ["ConnectionState"] = conn.State.ToString(),
                    ["Database"] = conn.Database,
                    ["ServerVersion"] = conn.ServerVersion,
                    ["DataSource"] = conn.DataSource,
                    ["ConnectionTimeout"] = conn.ConnectionTimeout
                };

                _logger.LogInformation("Database connection test successful");
                return new DbOperationResult(success: true, data: connectionInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TestConnection failed: {Message}", ex.Message);
            return new DbOperationResult(success: false, error: ex.Message);
        }
    }
}