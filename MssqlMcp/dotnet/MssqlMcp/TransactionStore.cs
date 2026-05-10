// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;

namespace Mssql.McpServer;

/// <summary>
/// In-memory store of open transactions, keyed by an opaque token returned to the caller.
/// Tokens identify a (connection, transaction) pair persisted across MCP calls so that
/// callers can run multiple statements atomically and decide to commit or rollback at the end.
/// </summary>
public sealed class TransactionStore
{
    private readonly ConcurrentDictionary<string, TxState> _store = new();

    public string Add(SqlConnection connection, SqlTransaction transaction)
    {
        var token = Guid.NewGuid().ToString("N");
        _store[token] = new TxState(connection, transaction, DateTimeOffset.UtcNow);
        return token;
    }

    public TxState? Get(string token) =>
        _store.TryGetValue(token, out var state) ? state : null;

    public TxState? Remove(string token) =>
        _store.TryRemove(token, out var state) ? state : null;

    public IReadOnlyList<(string Token, DateTimeOffset CreatedAt)> List() =>
        _store.Select(kv => (kv.Key, kv.Value.CreatedAt)).ToList();

    public sealed record TxState(SqlConnection Connection, SqlTransaction Transaction, DateTimeOffset CreatedAt);
}
