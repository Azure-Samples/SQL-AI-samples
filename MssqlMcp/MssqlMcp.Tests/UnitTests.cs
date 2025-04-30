using System.Threading.Tasks;
using Xunit;
using Moq;
using Mssql.McpServer;

namespace MssqlMcp.Tests
{
    public class MssqlMcpToolsTests
    {
        static MssqlMcpToolsTests()
        {
            // Ensure the test table does not exist before each test
            var _ = MssqlMcpTools.DropTable("DROP TABLE IF EXISTS TestTable").GetAwaiter().GetResult();
        }

        [Fact]
        public async Task CreateTable_ReturnsSuccess_WhenSqlIsValid()
        {
            // Arrange
            var sql = "CREATE TABLE TestTable (Id INT PRIMARY KEY)";
            // Act
            var result = await MssqlMcpTools.CreateTable(sql);
            // Assert
            Assert.NotNull(result);
            Assert.True((bool?)result.GetType().GetProperty("success")?.GetValue(result) ?? false);
        }

        [Fact]
        public async Task DescribeTable_ReturnsSchema_WhenTableExists()
        {
            var result = await MssqlMcpTools.DescribeTable("TestTable");
            Assert.NotNull(result);
            var type = result.GetType();
            // Should not be an error object
            var errorProp = type.GetProperty("error");
            Assert.True(errorProp == null || errorProp.GetValue(result) == null, $"DescribeTable returned error: {errorProp?.GetValue(result)}");
            // Should have table, columns, indexes, constraints
            var dict = result as System.Collections.IDictionary;
            Assert.NotNull(dict);
            Assert.True(dict.Contains("table"));
            Assert.True(dict.Contains("columns"));
            Assert.True(dict.Contains("indexes"));
            Assert.True(dict.Contains("constraints"));
            // Table info should have name and schema
            var table = dict["table"];
            Assert.NotNull(table);
            var tableType = table.GetType();
            Assert.NotNull(tableType.GetProperty("name"));
            Assert.NotNull(tableType.GetProperty("schema"));
            // Columns should be a list
            var columns = dict["columns"] as System.Collections.IEnumerable;
            Assert.NotNull(columns);
        }

        [Fact]
        public async Task DropTable_ReturnsSuccess_WhenSqlIsValid()
        {
            var sql = "DROP TABLE IF EXISTS TestTable";
            var result = await MssqlMcpTools.DropTable(sql);
            Assert.NotNull(result);
            Assert.True((bool?)result.GetType().GetProperty("success")?.GetValue(result) ?? false);
        }

        [Fact]
        public async Task InsertData_ReturnsSuccess_WhenSqlIsValid()
        {
            var sql = "INSERT INTO TestTable (Id) VALUES (1)";
            var result = await MssqlMcpTools.InsertData(sql);
            Assert.NotNull(result);
            Assert.True((bool?)result.GetType().GetProperty("success")?.GetValue(result) ?? false);
        }

        [Fact]
        public async Task ListTables_ReturnsTables()
        {
            var result = await MssqlMcpTools.ListTables();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ReadData_ReturnsData_WhenSqlIsValid()
        {
            var sql = "SELECT * FROM TestTable";
            var result = await MssqlMcpTools.ReadData(sql);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateData_ReturnsSuccess_WhenSqlIsValid()
        {
            var sql = "UPDATE TestTable SET Id = 2 WHERE Id = 1";
            var result = await MssqlMcpTools.UpdateData(sql);
            Assert.NotNull(result);
            Assert.True((bool?)result.GetType().GetProperty("success")?.GetValue(result) ?? false);
        }

        [Fact]
        public async Task CreateTable_ReturnsError_WhenSqlIsInvalid()
        {
            // Arrange
            var sql = "CREATE TABLE"; // Invalid SQL
            // Act
            var result = await MssqlMcpTools.CreateTable(sql);
            // Assert
            Assert.NotNull(result);
            var error = result.GetType().GetProperty("error")?.GetValue(result)?.ToString();
            Assert.Contains("syntax", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DescribeTable_ReturnsError_WhenTableDoesNotExist()
        {
            var result = await MssqlMcpTools.DescribeTable("NonExistentTable");
            Assert.NotNull(result);
            var error = result.GetType().GetProperty("error")?.GetValue(result)?.ToString();
            Assert.Contains("Table 'NonExistentTable' not found.", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DropTable_ReturnsError_WhenSqlIsInvalid()
        {
            var sql = "DROP"; // Invalid SQL
            var result = await MssqlMcpTools.DropTable(sql);
            Assert.NotNull(result);
            var error = result.GetType().GetProperty("error")?.GetValue(result)?.ToString();
            Assert.Contains("syntax", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task InsertData_ReturnsError_WhenSqlIsInvalid()
        {
            var sql = "INSERT INTO TestTable"; // Invalid SQL
            var result = await MssqlMcpTools.InsertData(sql);
            Assert.NotNull(result);
            var error = result.GetType().GetProperty("error")?.GetValue(result)?.ToString();
            Assert.Contains("syntax", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReadData_ReturnsError_WhenSqlIsInvalid()
        {
            var sql = "SELECT FROM"; // Invalid SQL
            var result = await MssqlMcpTools.ReadData(sql);
            Assert.NotNull(result);
            var error = result.GetType().GetProperty("error")?.GetValue(result)?.ToString();
            Assert.Contains("syntax", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateData_ReturnsError_WhenSqlIsInvalid()
        {
            var sql = "UPDATE TestTable"; // Invalid SQL
            var result = await MssqlMcpTools.UpdateData(sql);
            Assert.NotNull(result);
            var error = result.GetType().GetProperty("error")?.GetValue(result)?.ToString();
            Assert.Contains("syntax", error, StringComparison.OrdinalIgnoreCase);
        }
    }
}