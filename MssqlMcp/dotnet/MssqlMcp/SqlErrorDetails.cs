// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Mssql.McpServer;

/// <summary>
/// Structured details extracted from a <see cref="Microsoft.Data.SqlClient.SqlException"/>
/// so callers can react on the error number / state rather than parsing the message.
/// </summary>
public sealed class SqlErrorDetails
{
    public int Number { get; init; }
    public byte State { get; init; }
    public byte Class { get; init; }
    public int LineNumber { get; init; }
    public string? Procedure { get; init; }
    public string? Server { get; init; }
}
