// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;
public partial class Tools
{
    // Pre-compiled regex for dangerous keywords - much faster than creating regex objects on each call
    // Using word boundaries to avoid false positives (e.g., "UPDATED_AT" shouldn't match "UPDATE")
    private static readonly Regex DangerousKeywordsRegex = new(
        @"\b(DELETE|DROP|UPDATE|INSERT|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE|MERGE|REPLACE|" +
        @"GRANT|REVOKE|COMMIT|ROLLBACK|TRANSACTION|BEGIN|DECLARE|SET|USE|BACKUP|" +
        @"RESTORE|KILL|SHUTDOWN|WAITFOR|OPENROWSET|OPENDATASOURCE|OPENQUERY|OPENXML|BULK)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex patterns to detect common SQL injection techniques
    private static readonly Regex[] DangerousPatterns = 
    [
        // Semicolon followed by dangerous keywords
        new(@";\s*(DELETE|DROP|UPDATE|INSERT|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE|MERGE|REPLACE|GRANT|REVOKE)", RegexOptions.IgnoreCase),
        
        // UNION injection attempts with dangerous keywords
        new(@"UNION\s+(?:ALL\s+)?SELECT.*?(DELETE|DROP|UPDATE|INSERT|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE)", RegexOptions.IgnoreCase),
        
        // Comment-based injection attempts
        new(@"--.*?(DELETE|DROP|UPDATE|INSERT|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE)", RegexOptions.IgnoreCase),
        new(@"/\*.*?(DELETE|DROP|UPDATE|INSERT|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE).*?\*/", RegexOptions.IgnoreCase),
        
        // Stored procedure execution patterns
        new(@"EXEC\s*\(", RegexOptions.IgnoreCase),
        new(@"EXECUTE\s*\(", RegexOptions.IgnoreCase),
        new(@"sp_", RegexOptions.IgnoreCase),
        new(@"xp_", RegexOptions.IgnoreCase),
        
        // Bulk operations
        new(@"BULK\s+INSERT", RegexOptions.IgnoreCase),
        new(@"OPENROWSET", RegexOptions.IgnoreCase),
        new(@"OPENDATASOURCE", RegexOptions.IgnoreCase),
        
        // System functions that could be dangerous
        new(@"@@", RegexOptions.None),
        new(@"SYSTEM_USER", RegexOptions.IgnoreCase),
        new(@"USER_NAME", RegexOptions.IgnoreCase),
        new(@"DB_NAME", RegexOptions.IgnoreCase),
        new(@"HOST_NAME", RegexOptions.IgnoreCase),
        
        // Time delay attacks
        new(@"WAITFOR\s+DELAY", RegexOptions.IgnoreCase),
        new(@"WAITFOR\s+TIME", RegexOptions.IgnoreCase),
        
        // Multiple statements (semicolon not at end)
        new(@";\s*\w", RegexOptions.None),
        
        // String concatenation that might hide malicious code
        new(@"\+\s*CHAR\s*\(", RegexOptions.IgnoreCase),
        new(@"\+\s*NCHAR\s*\(", RegexOptions.IgnoreCase),
        new(@"\+\s*ASCII\s*\(", RegexOptions.IgnoreCase)
    ];

    /// <summary>
    /// Validates the SQL query for security issues
    /// </summary>
    /// <param name="query">The SQL query to validate</param>
    /// <returns>Validation result with success flag and error message if invalid</returns>
    private (bool IsValid, string? Error) ValidateQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return (false, "Query must be a non-empty string");
        }

        // Remove comments and normalize whitespace for analysis
        var cleanQuery = Regex.Replace(query, @"--.*$", "", RegexOptions.Multiline) // Remove line comments
            .Replace("/*", "").Replace("*/", "") // Remove block comments (simple approach)
            .Trim();

        cleanQuery = Regex.Replace(cleanQuery, @"\s+", " "); // Normalize whitespace

        if (string.IsNullOrWhiteSpace(cleanQuery))
        {
            return (false, "Query cannot be empty after removing comments");
        }

        var upperQuery = cleanQuery.ToUpperInvariant();

        // Must start with SELECT
        if (!upperQuery.StartsWith("SELECT"))
        {
            return (false, "Query must start with SELECT for security reasons");
        }

        // Check for dangerous keywords using our pre-compiled regex
        var match = DangerousKeywordsRegex.Match(cleanQuery);
        if (match.Success)
        {
            return (false, $"Dangerous keyword '{match.Value.ToUpper()}' detected in query. Only SELECT operations are allowed.");
        }

        // Check for dangerous patterns using regex
        foreach (var pattern in DangerousPatterns)
        {
            if (pattern.IsMatch(query))
            {
                return (false, "Potentially malicious SQL pattern detected. Only simple SELECT queries are allowed.");
            }
        }

        // Additional validation: Check for multiple statements
        var statements = cleanQuery.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (statements.Length > 1)
        {
            return (false, "Multiple SQL statements are not allowed. Use only a single SELECT statement.");
        }

        // Check for suspicious string patterns that might indicate obfuscation
        if (query.Contains("CHAR(") || query.Contains("NCHAR(") || query.Contains("ASCII("))
        {
            return (false, "Character conversion functions are not allowed as they may be used for obfuscation.");
        }

        // Limit query length to prevent potential DoS
        if (query.Length > 10000)
        {
            return (false, "Query is too long. Maximum allowed length is 10,000 characters.");
        }

        return (true, null);
    }

    /// <summary>
    /// Sanitizes the query result to prevent any potential security issues
    /// </summary>
    /// <param name="data">The query result data</param>
    /// <returns>Sanitized data</returns>
    private List<Dictionary<string, object?>> SanitizeResult(List<Dictionary<string, object?>> data)
    {
        // Limit the number of returned records to prevent memory issues
        const int maxRecords = 10000;
        if (data.Count > maxRecords)
        {
            _logger.LogWarning("Query returned {Count} records, limiting to {MaxRecords}", data.Count, maxRecords);
            data = data.Take(maxRecords).ToList();
        }

        // Early check: if no data or no suspicious characters in any column names, return as-is
        if (data.Count == 0 || !data[0].Keys.Any(key => Regex.IsMatch(key, @"[^\w\s\-_.]")))
        {
            return data;
        }

        return data.Select(record =>
        {
            var sanitized = new Dictionary<string, object?>();
            foreach (var (key, value) in record)
            {
                // Sanitize column names (remove any suspicious characters)
                var sanitizedKey = Regex.Replace(key, @"[^\w\s\-_.]", "");
                if (sanitizedKey != key)
                {
                    _logger.LogWarning("Column name sanitized: {Original} -> {Sanitized}", key, sanitizedKey);
                }
                sanitized[sanitizedKey] = value;
            }
            return sanitized;
        }).ToList();
    }

    [McpServerTool(
        Title = "Read Data",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Executes a SELECT query on an MSSQL Database table. The query must start with SELECT and cannot contain any destructive SQL operations for security reasons.")]
    public async Task<DbOperationResult> ReadData(
        [Description("SQL SELECT query to execute (must start with SELECT and cannot contain destructive operations). Example: SELECT * FROM movies WHERE genre = 'comedy'")] string sql)
    {
        // Validate the query for security issues
        var (isValid, error) = ValidateQuery(sql);
        if (!isValid)
        {
            _logger.LogWarning("Security validation failed for query: {QueryStart}...", sql?.Length > 100 ? sql[..100] : sql ?? "NULL");
            return new DbOperationResult(success: false, error: $"Security validation failed: {error}");
        }

        // Log the query for audit purposes (in production, consider more secure logging)
        _logger.LogInformation("Executing validated SELECT query: {QueryStart}{Truncated}", 
            sql.Length > 200 ? sql[..200] : sql, 
            sql.Length > 200 ? "..." : "");

        var conn = await _connectionFactory.GetOpenConnectionAsync();
        try
        {
            using (conn)
            {
                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                var results = new List<Dictionary<string, object?>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);
                }

                // Sanitize the result
                var sanitizedResults = SanitizeResult(results);
                
                return new DbOperationResult(
                    success: true, 
                    data: sanitizedResults);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReadData failed: {Message}", ex.Message);
            
            // Don't expose internal error details to prevent information leakage
            var safeErrorMessage = ex.Message.Contains("Invalid object name")
                ? ex.Message
                : "Database query execution failed";
            
            return new DbOperationResult(success: false, error: $"Failed to execute query: {safeErrorMessage}");
        }
    }
}