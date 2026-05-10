// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Bulk Insert",
        ReadOnly = false,
        Destructive = false),
        Description("Bulk-inserts a list of rows into a destination table using SqlBulkCopy. Each row is a dictionary of column name to value. Column names in the first row define the mapping.")]
    public Task<DbOperationResult> BulkInsert(
        [Description("Destination table (optionally schema-qualified, e.g. 'dbo.Users')")] string table,
        [Description("Rows to insert. Each row is { columnName: value }. The first row's keys define the column set.")] List<Dictionary<string, object?>> rows,
        [Description("Optional batch size (rows per network round-trip). Default: 1000.")] int? batchSize = null,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            if (rows.Count == 0)
            {
                return DbOperationResult.Ok(rowsAffected: 0);
            }

            var firstRow = rows[0];
            using var dt = new DataTable();
            foreach (var key in firstRow.Keys)
            {
                dt.Columns.Add(key, typeof(object));
            }

            foreach (var row in rows)
            {
                var dr = dt.NewRow();
                foreach (var key in firstRow.Keys)
                {
                    var raw = row.TryGetValue(key, out var v) ? v : null;
                    dr[key] = SqlParameterBinding.Unwrap(raw) ?? DBNull.Value;
                }
                dt.Rows.Add(dr);
            }

            using var bulk = new SqlBulkCopy(conn)
            {
                DestinationTableName = table,
                BatchSize = batchSize ?? 1000,
                BulkCopyTimeout = _options.CommandTimeoutSeconds,
            };
            foreach (var key in firstRow.Keys)
            {
                bulk.ColumnMappings.Add(key, key);
            }

            await bulk.WriteToServerAsync(dt, ct);
            return DbOperationResult.Ok(
                data: new { table, inserted = rows.Count },
                rowsAffected: rows.Count);
        }, database, cancellationToken, requiresWrite: true);
    }
}
