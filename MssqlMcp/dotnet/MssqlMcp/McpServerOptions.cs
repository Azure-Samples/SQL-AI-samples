// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Mssql.McpServer;

/// <summary>
/// Server-wide options sourced from environment variables at startup.
/// </summary>
public sealed class McpServerOptions
{
    public int CommandTimeoutSeconds { get; init; } = 30;

    public int MaxRows { get; init; } = 1000;

    public bool ReadOnly { get; init; }

    public IReadOnlySet<string>? AllowedSchemas { get; init; }

    public static McpServerOptions FromEnvironment()
    {
        return new McpServerOptions
        {
            CommandTimeoutSeconds = ReadInt("MSSQLMCP_COMMAND_TIMEOUT", 30),
            MaxRows = ReadInt("MSSQLMCP_MAX_ROWS", 1000),
            ReadOnly = ReadBool("MSSQLMCP_READ_ONLY", false),
            AllowedSchemas = ReadSet("MSSQLMCP_ALLOWED_SCHEMAS"),
        };
    }

    private static int ReadInt(string name, int defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : defaultValue;
    }

    private static bool ReadBool(string name, bool defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return bool.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }

    private static IReadOnlySet<string>? ReadSet(string name)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }
        return raw
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
