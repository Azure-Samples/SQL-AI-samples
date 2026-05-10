// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Commit Transaction",
        ReadOnly = false,
        Destructive = false),
        Description("Commits an open transaction identified by its token and closes the underlying connection.")]
    public async Task<DbOperationResult> CommitTransaction(
        [Description("Transaction token returned by BeginTransaction")] string transactionToken,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var state = _transactionStore.Remove(transactionToken);
        if (state is null)
        {
            return DbOperationResult.Fail($"Transaction token '{transactionToken}' not found.");
        }

        try
        {
            await state.Transaction.CommitAsync(cancellationToken);
            sw.Stop();
            return DbOperationResult.Ok(durationMs: sw.ElapsedMilliseconds);
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            sw.Stop();
            _logger.LogError(sqlEx, "CommitTransaction failed: {Message}", sqlEx.Message);
            return DbOperationResult.FromSqlException(sqlEx, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "CommitTransaction failed: {Message}", ex.Message);
            return DbOperationResult.Fail(ex.Message, sw.ElapsedMilliseconds);
        }
        finally
        {
            await state.Transaction.DisposeAsync();
            await state.Connection.DisposeAsync();
        }
    }
}
