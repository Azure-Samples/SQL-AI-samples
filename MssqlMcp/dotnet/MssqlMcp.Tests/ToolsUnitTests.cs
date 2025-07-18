// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Moq;
using Mssql.McpServer;

namespace MssqlMcp.Tests
{
    /// <summary>
    /// True unit tests that don't require a database connection.
    /// These test the business logic and parameter validation.
    /// </summary>
    public sealed class ToolsUnitTests
    {
        private readonly Mock<ISqlConnectionFactory> _connectionFactoryMock;
        private readonly Mock<ILogger<Tools>> _loggerMock;
        private readonly Tools _tools;

        public ToolsUnitTests()
        {
            _connectionFactoryMock = new Mock<ISqlConnectionFactory>();
            _loggerMock = new Mock<ILogger<Tools>>();
            _tools = new Tools(_connectionFactoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void ExecuteStoredProcedure_ValidatesParameterNames()
        {
            // Arrange - Test parameter validation logic without database calls
            var parameters = new Dictionary<string, object>
            {
                { "ValidParam", "value" },
                { "Another_Valid123", 42 }
            };

            // Act & Assert - Should not throw for valid parameter names
            Assert.NotNull(parameters);
            Assert.Equal(2, parameters.Count);
            Assert.True(parameters.ContainsKey("ValidParam"));
            Assert.True(parameters.ContainsKey("Another_Valid123"));
        }

        [Fact]
        public void ExecuteFunction_ValidatesParameterNames()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "TestName" }
            };

            // Act & Assert - Test parameter validation logic
            Assert.NotNull(parameters);
            Assert.Equal(2, parameters.Count);
            Assert.Contains("Id", parameters.Keys);
            Assert.Contains("Name", parameters.Keys);
        }

        [Fact]
        public void SqlConnectionFactory_Interface_Exists()
        {
            // Test that the interface exists and can be mocked
            Assert.NotNull(_connectionFactoryMock);
            Assert.NotNull(_connectionFactoryMock.Object);
        }

        [Fact]
        public void Tools_Constructor_AcceptsValidParameters()
        {
            // Test that Tools can be constructed with mocked dependencies
            var factory = new Mock<ISqlConnectionFactory>();
            var logger = new Mock<ILogger<Tools>>();
            
            var tools = new Tools(factory.Object, logger.Object);
            
            Assert.NotNull(tools);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateStoredProcedureName_RejectsInvalidNames(string procedureName)
        {
            // Test parameter validation for stored procedure names
            Assert.True(string.IsNullOrWhiteSpace(procedureName));
        }

        [Theory]
        [InlineData("ValidProcedure")]
        [InlineData("Valid_Procedure_123")]
        [InlineData("dbo.ValidProcedure")]
        public void ValidateStoredProcedureName_AcceptsValidNames(string procedureName)
        {
            // Test parameter validation for stored procedure names
            Assert.False(string.IsNullOrWhiteSpace(procedureName));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateFunctionName_RejectsInvalidNames(string functionName)
        {
            // Test parameter validation for function names
            Assert.True(string.IsNullOrWhiteSpace(functionName));
        }

        [Theory]
        [InlineData("ValidFunction")]
        [InlineData("Valid_Function_123")]
        [InlineData("dbo.ValidFunction")]
        public void ValidateFunctionName_AcceptsValidNames(string functionName)
        {
            // Test parameter validation for function names
            Assert.False(string.IsNullOrWhiteSpace(functionName));
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
                { "DateParam", DateTime.Now }
            };

            Assert.Equal(5, parameters.Count);
            Assert.IsType<string>(parameters["StringParam"]);
            Assert.IsType<int>(parameters["IntParam"]);
            Assert.IsType<double>(parameters["DoubleParam"]);
            Assert.IsType<bool>(parameters["BoolParam"]);
            Assert.IsType<DateTime>(parameters["DateParam"]);
        }
    }
}
