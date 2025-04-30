using System.Data;
using Microsoft.Data.SqlClient;

namespace Mssql.McpServer
{
    public static class SqlConnectionManager
    {
        private static SqlConnection? _sharedConnection;
        private static readonly object _connectionLock = new();

        public static string GetSqlConfig()
        {
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string is not set in the environment variable 'CONNECTION_STRING'.");
            }
            return connectionString;
        }

        public static async Task<(SqlConnection? conn, object? errorResult)> GetOpenConnectionAsync()
        {
            lock (_connectionLock)
            {
                if (_sharedConnection != null && _sharedConnection.State == ConnectionState.Open)
                {
                    return (_sharedConnection, (object?)null);
                }
            }

            var connectionString = GetSqlConfig();
            try
            {
                var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                lock (_connectionLock)
                {
                    _sharedConnection = conn;
                }
                return (conn, null);
            }
            catch (Exception ex)
            {
                return (null, new { success = false, error = ex.Message });
            }
        }
    }
}
