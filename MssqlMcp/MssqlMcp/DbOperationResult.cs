// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Mssql.McpServer;

public class DbOperationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int? RowsAffected { get; set; }
    public object? Data { get; set; }
}
