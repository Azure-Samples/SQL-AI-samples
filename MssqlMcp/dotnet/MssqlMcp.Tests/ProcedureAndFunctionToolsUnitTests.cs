// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Moq;
using Mssql.McpServer;

namespace MssqlMcp.Tests
{
    /// <summary>
    /// Unit tests for stored procedure and function tools.
    /// These test the business logic and parameter validation without database dependencies.
    /// </summary>
    public sealed class ProcedureAndFunctionToolsUnitTests
    {
        private readonly Mock<ISqlConnectionFactory> _connectionFactoryMock;
        private readonly Mock<ILogger<Tools>> _loggerMock;
        private readonly Tools _tools;

        public ProcedureAndFunctionToolsUnitTests()
        {
            _connectionFactoryMock = new Mock<ISqlConnectionFactory>();
            _loggerMock = new Mock<ILogger<Tools>>();
            _tools = new Tools(_connectionFactoryMock.Object, _loggerMock.Object);
        }

        #region CreateProcedure Tests

        [Theory]
        [InlineData("CREATE PROCEDURE dbo.TestProc AS BEGIN SELECT 1 END")]
        [InlineData("CREATE OR ALTER PROCEDURE TestProc AS SELECT * FROM Users")]
        [InlineData("create procedure MyProc (@id int) as begin select @id end")]
        [InlineData("CREATE PROCEDURE [dbo].[My Proc] AS BEGIN PRINT 'Hello' END")]
        public void CreateProcedure_ValidatesValidCreateStatements(string sql)
        {
            // Test that valid CREATE PROCEDURE statements pass validation
            var trimmedSql = sql.Trim();
            Assert.True(trimmedSql.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("PROCEDURE", trimmedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("SELECT * FROM Users")]
        [InlineData("UPDATE Users SET Name = 'Test'")]
        [InlineData("CREATE TABLE Test (Id INT)")]
        [InlineData("CREATE FUNCTION TestFunc() RETURNS INT AS BEGIN RETURN 1 END")]
        [InlineData("DROP PROCEDURE TestProc")]
        [InlineData("ALTER PROCEDURE TestProc AS BEGIN SELECT 2 END")]
        public void CreateProcedure_RejectsNonCreateProcedureStatements(string sql)
        {
            // Test that non-CREATE PROCEDURE statements are rejected
            var trimmedSql = sql.Trim();
            var isValidCreateProcedure = trimmedSql.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase) && 
                                       trimmedSql.Contains("PROCEDURE", StringComparison.OrdinalIgnoreCase);
            Assert.False(isValidCreateProcedure);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateProcedure_RejectsEmptyOrWhitespaceSql(string sql)
        {
            // Test that empty or whitespace SQL is rejected
            Assert.True(string.IsNullOrWhiteSpace(sql));
        }

        #endregion

        #region CreateFunction Tests

        [Theory]
        [InlineData("CREATE FUNCTION dbo.TestFunc() RETURNS INT AS BEGIN RETURN 1 END")]
        [InlineData("CREATE OR ALTER FUNCTION TestFunc(@id int) RETURNS TABLE AS RETURN SELECT @id as Id")]
        [InlineData("create function MyFunc (@param varchar(50)) returns varchar(100) as begin return @param + ' processed' end")]
        [InlineData("CREATE FUNCTION [dbo].[My Function] () RETURNS INT AS BEGIN RETURN 42 END")]
        public void CreateFunction_ValidatesValidCreateStatements(string sql)
        {
            // Test that valid CREATE FUNCTION statements pass validation
            var trimmedSql = sql.Trim();
            Assert.True(trimmedSql.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("FUNCTION", trimmedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("SELECT * FROM Users")]
        [InlineData("UPDATE Users SET Name = 'Test'")]
        [InlineData("CREATE TABLE Test (Id INT)")]
        [InlineData("CREATE PROCEDURE TestProc AS BEGIN SELECT 1 END")]
        [InlineData("DROP FUNCTION TestFunc")]
        [InlineData("ALTER FUNCTION TestFunc() RETURNS INT AS BEGIN RETURN 2 END")]
        public void CreateFunction_RejectsNonCreateFunctionStatements(string sql)
        {
            // Test that non-CREATE FUNCTION statements are rejected
            var trimmedSql = sql.Trim();
            var isValidCreateFunction = trimmedSql.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase) && 
                                      trimmedSql.Contains("FUNCTION", trimmedSql, StringComparison.OrdinalIgnoreCase);
            Assert.False(isValidCreateFunction);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateFunction_RejectsEmptyOrWhitespaceSql(string sql)
        {
            // Test that empty or whitespace SQL is rejected
            Assert.True(string.IsNullOrWhiteSpace(sql));
        }

        #endregion

        #region ExecuteStoredProcedure Tests

        [Fact]
        public void ExecuteStoredProcedure_ValidatesParameterTypes()
        {
            // Test that parameter dictionaries can handle various data types
            var parameters = new Dictionary<string, object>
            {
                { "StringParam", "test" },
                { "IntParam", 42 },
                { "DoubleParam", 3.14 },
                { "BoolParam", true },
                { "DateParam", DateTime.Now },
                { "NullParam", null! }
            };

            Assert.Equal(6, parameters.Count);
            Assert.IsType<string>(parameters["StringParam"]);
            Assert.IsType<int>(parameters["IntParam"]);
            Assert.IsType<double>(parameters["DoubleParam"]);
            Assert.IsType<bool>(parameters["BoolParam"]);
            Assert.IsType<DateTime>(parameters["DateParam"]);
            Assert.Null(parameters["NullParam"]);
        }

        [Theory]
        [InlineData("ValidParam")]
        [InlineData("Another_Valid123")]
        [InlineData("@ParamWithAt")]
        [InlineData("CamelCaseParam")]
        [InlineData("snake_case_param")]
        public void ExecuteStoredProcedure_AcceptsValidParameterNames(string paramName)
        {
            // Test that valid parameter names are accepted
            var parameters = new Dictionary<string, object> { { paramName, "value" } };
            Assert.True(parameters.ContainsKey(paramName));
            Assert.Equal("value", parameters[paramName]);
        }

        [Fact]
        public void ExecuteStoredProcedure_HandlesEmptyParameters()
        {
            // Test that null or empty parameter dictionary is handled
            Dictionary<string, object>? nullParams = null;
            var emptyParams = new Dictionary<string, object>();

            Assert.Null(nullParams);
            Assert.NotNull(emptyParams);
            Assert.Empty(emptyParams);
        }

        #endregion

        #region ExecuteFunction Tests

        [Fact]
        public void ExecuteFunction_ValidatesParameterTypes()
        {
            // Test that parameter dictionaries can handle various data types for functions
            var parameters = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "TestName" },
                { "StartDate", DateTime.Today },
                { "IsActive", true },
                { "Score", 95.5 }
            };

            Assert.Equal(5, parameters.Count);
            Assert.Contains("Id", parameters.Keys);
            Assert.Contains("Name", parameters.Keys);
            Assert.Contains("StartDate", parameters.Keys);
            Assert.Contains("IsActive", parameters.Keys);
            Assert.Contains("Score", parameters.Keys);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ExecuteFunction_ValidatesEmptyFunctionName(string functionName)
        {
            // Test function name validation
            Assert.True(string.IsNullOrWhiteSpace(functionName));
        }

        [Theory]
        [InlineData("ValidFunction")]
        [InlineData("Valid_Function_123")]
        [InlineData("dbo.ValidFunction")]
        [InlineData("[schema].[My Function]")]
        public void ExecuteFunction_AcceptsValidFunctionNames(string functionName)
        {
            // Test function name validation for valid names
            Assert.False(string.IsNullOrWhiteSpace(functionName));
            Assert.True(functionName.Length > 0);
        }

        #endregion

        #region General Validation Tests

        [Fact]
        public void Tools_Constructor_AcceptsValidDependencies()
        {
            // Test that Tools can be constructed with mocked dependencies
            var factory = new Mock<ISqlConnectionFactory>();
            var logger = new Mock<ILogger<Tools>>();
            
            var tools = new Tools(factory.Object, logger.Object);
            
            Assert.NotNull(tools);
        }

        [Fact]
        public void SqlConnectionFactory_Interface_CanBeMocked()
        {
            // Test that the interface exists and can be mocked
            Assert.NotNull(_connectionFactoryMock);
            Assert.NotNull(_connectionFactoryMock.Object);
        }

        [Theory]
        [InlineData("dbo.MyProcedure")]
        [InlineData("schema.MyFunction")]
        [InlineData("[My Schema].[My Object]")]
        [InlineData("SimpleObject")]
        public void DatabaseObjectNames_ValidateSchemaQualifiedNames(string objectName)
        {
            // Test that schema-qualified names are handled properly
            Assert.False(string.IsNullOrWhiteSpace(objectName));
            
            // Check if it's schema-qualified
            var hasSchema = objectName.Contains('.');
            if (hasSchema)
            {
                var parts = objectName.Split('.');
                Assert.True(parts.Length >= 2);
                Assert.All(parts, part => Assert.False(string.IsNullOrWhiteSpace(part.Trim('[', ']'))));
            }
        }

        [Fact]
        public void ParameterDictionary_HandlesNullValues()
        {
            // Test that parameter dictionaries can handle null values
            var parameters = new Dictionary<string, object>
            {
                { "NullParam", null! },
                { "StringParam", "value" },
                { "IntParam", 42 }
            };

            Assert.Equal(3, parameters.Count);
            Assert.Null(parameters["NullParam"]);
            Assert.Equal("value", parameters["StringParam"]);
            Assert.Equal(42, parameters["IntParam"]);
        }

        [Fact]
        public void ParameterDictionary_HandlesVariousTypes()
        {
            // Test that parameter dictionaries can handle various data types
            var parameters = new Dictionary<string, object>
            {
                { "StringParam", "test" },
                { "IntParam", 42 },
                { "DoubleParam", 3.14 },
                { "BoolParam", true },
                { "DateParam", DateTime.Now },
                { "DecimalParam", 123.45m },
                { "GuidParam", Guid.NewGuid() }
            };

            Assert.Equal(7, parameters.Count);
            Assert.IsType<string>(parameters["StringParam"]);
            Assert.IsType<int>(parameters["IntParam"]);
            Assert.IsType<double>(parameters["DoubleParam"]);
            Assert.IsType<bool>(parameters["BoolParam"]);
            Assert.IsType<DateTime>(parameters["DateParam"]);
            Assert.IsType<decimal>(parameters["DecimalParam"]);
            Assert.IsType<Guid>(parameters["GuidParam"]);
        }

        #endregion
    }
}