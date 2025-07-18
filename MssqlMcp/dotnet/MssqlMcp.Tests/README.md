# Test Documentation

This project contains two types of tests to ensure comprehensive coverage:

## Unit Tests (`ToolsUnitTests.cs`)
**Purpose**: Fast, isolated tests that don't require external dependencies.

- ✅ **No database required** - Run anywhere, anytime
- ✅ **Fast execution** - Complete in seconds  
- ✅ **Parameter validation** - Test input validation logic
- ✅ **Business logic** - Test pure functions and data structures
- ✅ **Mocking** - Test interfaces and dependency injection

**Run unit tests only:**
```bash
dotnet test --filter "FullyQualifiedName~ToolsUnitTests"
```

## Integration Tests (`UnitTests.cs` -> `MssqlMcpTests`)
**Purpose**: End-to-end testing with real SQL Server database.

- 🔌 **Database required** - Tests full SQL Server integration
- 📊 **Real data operations** - Creates tables, stored procedures, functions
- 🧪 **Complete workflows** - Tests actual MCP tool execution
- ⚡ **14 original tests** - Core CRUD and error handling scenarios

**Prerequisites for integration tests:**
1. SQL Server running locally
2. Database named 'test' 
3. Set environment variable:
   ```bash
   SET CONNECTION_STRING=Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True
   ```

**Run integration tests only:**
```bash
dotnet test --filter "FullyQualifiedName~MssqlMcpTests"
```

**Run all tests:**
```bash
dotnet test
```

## Test Coverage

### ExecuteStoredProcedure Tool
- ✅ Unit: Parameter validation and structure
- ⚠️ Integration: **Not included** - Use unit tests for validation

### ExecuteFunction Tool  
- ✅ Unit: Parameter validation and structure
- ⚠️ Integration: **Not included** - Use unit tests for validation

### All Other Tools
- ✅ Unit: Interface and dependency validation
- ✅ Integration: Full CRUD operations with real database (14 tests)

## Best Practices

1. **Run unit tests during development** - They're fast and catch logic errors
2. **Run integration tests before commits** - They verify end-to-end functionality  
3. **Use unit tests for TDD** - Write failing unit tests, then implement features
4. **Use integration tests for deployment validation** - Verify database connectivity

This approach follows the **Test Pyramid** principle:
- Many fast unit tests (base of pyramid)
- Fewer comprehensive integration tests (top of pyramid)
