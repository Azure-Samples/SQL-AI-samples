// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Mssql.McpServer;

/// <summary>
/// Binds a dictionary of named parameter values onto a <see cref="SqlCommand"/>.
/// Supports raw CLR values and <see cref="JsonElement"/> values (as deserialized by the MCP SDK).
/// Names are normalized to start with '@'.
/// </summary>
public static class SqlParameterBinding
{
    public static void Apply(SqlCommand cmd, IDictionary<string, object?>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
        {
            return;
        }

        foreach (var (key, raw) in parameters)
        {
            var name = key.StartsWith('@') ? key : '@' + key;
            var value = Unwrap(raw);
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }

    public static object? Unwrap(object? value) => value switch
    {
        null => null,
        JsonElement je => UnwrapJson(je),
        _ => value,
    };

    private static object? UnwrapJson(JsonElement je) => je.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => je.GetString(),
        JsonValueKind.Number => je.TryGetInt64(out var l) ? (object)l : je.GetDouble(),
        _ => je.GetRawText(),
    };
}
