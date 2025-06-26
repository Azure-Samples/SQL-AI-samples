// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Moq;
using Mssql.McpServer;

namespace MssqlMcp.Tests
{
    public sealed class MssqlMcpTests : IDisposable
    {
        private readonly string _tableName;
        private readonly Tools _tools;
        public MssqlMcpTests()
        {
            _tableName = $"TestTable_{Guid.NewGuid():N}";
            var connectionFactory = new SqlConnectionFactory();
            var loggerMock = new Mock<ILogger<Tools>>();
            _tools = new Tools(connectionFactory, loggerMock.Object);
        }

        public void Dispose()
        {
            // Cleanup: Drop the table after each test
            var _ = _tools.DropTable($"DROP TABLE IF EXISTS {_tableName}").GetAwaiter().GetResult();
        }

        [Fact]
        public async Task CreateTable_ReturnsSuccess_WhenSqlIsValid()
        {
            var sql = $"CREATE TABLE {_tableName} (Id INT PRIMARY KEY)";
            var result = await _tools.CreateTable(sql) as DbOperationResult;
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task DescribeTable_ReturnsSchema_WhenTableExists()
        {
            // Ensure table exists
            var createResult = await _tools.CreateTable($"CREATE TABLE {_tableName} (Id INT PRIMARY KEY)") as DbOperationResult;
            Assert.NotNull(createResult);
            Assert.True(createResult.Success);

            var result = await _tools.DescribeTable(_tableName) as DbOperationResult;
            Assert.NotNull(result);
            Assert.True(result.Success);
            var dict = result.Data as System.Collections.IDictionary;
            Assert.NotNull(dict);
            Assert.True(dict.Contains("table"));
            Assert.True(dict.Contains("columns"));
            Assert.True(dict.Contains("indexes"));
            Assert.True(dict.Contains("constraints"));
            var table = dict["table"];
            Assert.NotNull(table);
            var tableType = table.GetType();
            Assert.NotNull(tableType.GetProperty("name"));
            Assert.NotNull(tableType.GetProperty("schema"));
            var columns = dict["columns"] as System.Collections.IEnumerable;
            Assert.NotNull(columns);
        }

        [Fact]
        public async Task DropTable_ReturnsSuccess_WhenSqlIsValid()
        {
            var sql = $"DROP TABLE IF EXISTS {_tableName}";
            var result = await _tools.DropTable(sql) as DbOperationResult;
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task InsertData_ReturnsSuccess_WhenSqlIsValid()
        {
            // Ensure table exists
            var createResult = await _tools.CreateTable($"CREATE TABLE {_tableName} (Id INT PRIMARY KEY)") as DbOperationResult;
            Assert.NotNull(createResult);
            Assert.True(createResult.Success);

            var sql = $"INSERT INTO {_tableName} (Id) VALUES (1)";
            var result = await _tools.InsertData(sql) as DbOperationResult;
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(result.RowsAffected.HasValue && result.RowsAffected.Value > 0);
        }

        [Fact]
        public async Task ListTables_ReturnsTables()
        {
            var result = await _tools.ListTables() as DbOperationResult;
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task ReadData_ReturnsData_WhenSqlIsValid()
        {
            // Ensure table exists and has data
            var createResult = await _tools.CreateTable($"CREATE TABLE {_tableName} (Id INT PRIMARY KEY)") as DbOperationResult;
            Assert.NotNull(createResult);
            Assert.True(createResult.Success);
            var insertResult = await _tools.InsertData($"INSERT INTO {_tableName} (Id) VALUES (1)") as DbOperationResult;
            Assert.NotNull(insertResult);
            Assert.True(insertResult.Success);

            var sql = $"SELECT * FROM {_tableName}";
            var result = await _tools.ReadData(sql) as DbOperationResult;
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task UpdateData_ReturnsSuccess_WhenSqlIsValid()
        {
            // Ensure table exists and has data
            var createResult = await _tools.CreateTable($"CREATE TABLE {_tableName} (Id INT PRIMARY KEY)") as DbOperationResult;
            Assert.NotNull(createResult);
            Assert.True(createResult.Success);
            var insertResult = await _tools.InsertData($"INSERT INTO {_tableName} (Id) VALUES (1)") as DbOperationResult;
            Assert.NotNull(insertResult);
            Assert.True(insertResult.Success);

            var sql = $"UPDATE {_tableName} SET Id = 2 WHERE Id = 1";
            var result = await _tools.UpdateData(sql) as DbOperationResult;
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(result.RowsAffected.HasValue);
        }

        [Fact]
        public async Task CreateTable_ReturnsError_WhenSqlIsInvalid()
        {
            var sql = "CREATE TABLE";
            var result = await _tools.CreateTable(sql) as DbOperationResult;
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("syntax", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DescribeTable_ReturnsError_WhenTableDoesNotExist()
        {
            var result = await _tools.DescribeTable("NonExistentTable") as DbOperationResult;
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Table 'NonExistentTable' not found.", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DropTable_ReturnsError_WhenSqlIsInvalid()
        {
            var sql = "DROP";
            var result = await _tools.DropTable(sql) as DbOperationResult;
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("syntax", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task InsertData_ReturnsError_WhenSqlIsInvalid()
        {
            var sql = "INSERT INTO TestTable";
            var result = await _tools.InsertData(sql) as DbOperationResult;
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("syntax", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReadData_ReturnsError_WhenSqlIsInvalid()
        {
            var sql = "SELECT FROM";
            var result = await _tools.ReadData(sql) as DbOperationResult;
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("syntax", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateData_ReturnsError_WhenSqlIsInvalid()
        {
            var sql = "UPDATE TestTable";
            var result = await _tools.UpdateData(sql) as DbOperationResult;
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("syntax", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SqlInjection_NotExecuted_When_QueryFails()
        {
            // Ensure table exists
            var createResult = await _tools.CreateTable($"CREATE TABLE {_tableName} (Id INT PRIMARY KEY, Name NVARCHAR(100))") as DbOperationResult;
            Assert.NotNull(createResult);
            Assert.True(createResult.Success);

            // Attempt SQL Injection
            var maliciousInput = "1; DROP TABLE " + _tableName + "; --";
            var sql = $"INSERT INTO {_tableName} (Id, Name) VALUES ({maliciousInput}, 'Malicious')";
            var result = await _tools.InsertData(sql) as DbOperationResult;

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("syntax", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            // Verify table still exists
            var describeResult = await _tools.DescribeTable(_tableName) as DbOperationResult;
            Assert.NotNull(describeResult);
            Assert.True(describeResult.Success);
        }
    }
}