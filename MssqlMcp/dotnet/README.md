# Mssql SQL MCP Server (.NET 8)

This project is a .NET 8 console application implementing a Model Context Protocol (MCP) server for MSSQL Databases using the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).

## Features

- Provide connection string via environment variable `CONNECTION_STRING`.
- **MCP Tools Implemented**:
  - **Table Operations**:
    - ListTables: List all tables in the database.
    - DescribeTable: Get schema/details for a table.
    - CreateTable: Create new tables.
    - DropTable: Drop existing tables.
  - **Data Operations**:
    - InsertData: Insert data into tables.
    - ReadData: Read/query data from tables.
    - UpdateData: Update values in tables.
  - **Stored Procedure & Function Operations**:
    - CreateProcedure: Create new stored procedures.
    - CreateFunction: Create new functions (scalar and table-valued).
    - ExecuteStoredProcedure: Execute stored procedures with optional parameters.
    - ExecuteFunction: Execute table-valued functions with optional parameters.
    - ListProceduresAndFunctions: List all stored procedures and functions in the database.
    - DescribeProcedureOrFunction: Get detailed metadata about specific procedures/functions.
- **Logging**: Console logging using Microsoft.Extensions.Logging.
- **Comprehensive Testing**: 
  - **Unit Tests**: Fast, database-independent tests using mocks (22+ tests)
  - **Integration Tests**: End-to-end testing with real SQL Server (14+ tests)

## Testing

The project includes two types of tests following the Test Pyramid principle:

### Unit Tests (`ToolsUnitTests.cs`)
**Purpose**: Fast, isolated tests that don't require external dependencies.

- ✅ **No database required** - Run anywhere, anytime
- ✅ **Fast execution** - Complete in seconds  
- ✅ **Parameter validation** - Test input validation logic
- ✅ **Business logic** - Test pure functions and data structures
- ✅ **22+ tests** covering all new stored procedure and function tools

**Run unit tests only:**
```bash
dotnet test --filter "FullyQualifiedName~ToolsUnitTests"
```

### Integration Tests (`UnitTests.cs` -> `MssqlMcpTests`)
**Purpose**: End-to-end testing with real SQL Server database.

- 🔌 **Database required** - Tests full SQL Server integration
- 📊 **Real data operations** - Creates tables, stored procedures, functions
- 🧪 **Complete workflows** - Tests actual MCP tool execution
- ⚡ **14+ tests** - Core CRUD and error handling scenarios

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

## Getting Started

### Prerequisites

- Access to a SQL Server or Azure SQL Database

### Setup

1. **Build**

---
```sh
   cd MssqlMcp
   dotnet build
```
---

2. VSCode: **Start VSCode, and add MCP Server config to VSCode Settings**

Load the settings file in VSCode (Ctrl+Shift+P > Preferences: Open Settings (JSON)).

Add a new MCP Server with the following settings:

---
```json
    "MSSQL MCP": {
        "type": "stdio",
        "command": "C:\\src\\MssqlMcp\\MssqlMcp\\bin\\Debug\\net8.0\\MssqlMcp.exe",
        "env": {
            "CONNECTION_STRING": "Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True"
            }
}
```
---

NOTE: Replace the path "C:\\src\\SQL-AI-samples" with the location of your SQL-AI-samples repo on your machine.

e.g. your MCP settings should look like this if "MSSQL MCP" is your own MCP Server in VSCode settings:

---
```json
"mcp": {
    "servers": {
        "MSSQL MCP": {
            "type": "stdio",
            "command": "C:\\src\\SQL-AI-samples\\MssqlMcp\\MssqlMcp\\bin\\Debug\\net8.0\\MssqlMcp.exe",
                "env": {
                "CONNECTION_STRING": "Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True"
            }
    }
}
```
---

An example of using a connection string for Azure SQL Database:
---
```json
"mcp": {
    "servers": {
        "MSSQL MCP": {
            "type": "stdio",
            "command": "C:\\src\\SQL-AI-samples\\MssqlMcp\\MssqlMcp\\bin\\Debug\\net8.0\\MssqlMcp.exe",
                "env": {
                "CONNECTION_STRING": "Server=tcp:<servername>.database.windows.net,1433;Initial Catalog=<databasename>;Encrypt=Mandatory;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Interactive"
            }
    }
}
```
---

**Run the MCP Server**

Save the Settings file, and then you should see the "Start" button appear in the settings.json.  Click "Start" to start the MCP Server. (You can then click on "Running" to view the Output window).

Start Chat (Ctrl+Shift+I), make sure Agent Mode is selected.

Click the tools icon, and ensure the "MSSQL MCP" tools are selected.

Then type in the chat window "List tables in the database" and hit enter. (If you have other tools loaded, you may need to specify "MSSQL MCP" in the initial prompt, e.g. "Using MSSQL MCP, list tables").

3. Claude Desktop: **Add MCP Server config to Claude Desktop**

Press File > Settings > Developer.
Press the "Edit Config" button (which will load the claude_desktop_config.json file in your editor).

Add a new MCP Server with the following settings:

---
```json
{
    "mcpServers": {
        "MSSQL MCP": {
            "command": "C:\\src\\SQL-AI-samples\\MssqlMcp\\MssqlMcp\\bin\\Debug\\net8.0\\MssqlMcp.exe",
            "env": {
                    "CONNECTION_STRING": "Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True"
                }
        }
    }
}
```
---

Save the file, start a new Chat, you'll see the "Tools" icon, it should list **13 MSSQL MCP tools** (7 original + 6 new stored procedure/function tools).

## Available MCP Tools

### Table Operations (7 tools)
1. **ListTables** - List all tables in the database
2. **DescribeTable** - Get schema/details for a table
3. **CreateTable** - Create new tables
4. **DropTable** - Drop existing tables
5. **InsertData** - Insert data into tables
6. **ReadData** - Read/query data from tables
7. **UpdateData** - Update values in tables

### Stored Procedure & Function Operations (6 tools)
8. **CreateProcedure** - Create new stored procedures with full SQL support
9. **CreateFunction** - Create new functions (scalar and table-valued)
10. **ExecuteStoredProcedure** - Execute stored procedures with optional parameters
11. **ExecuteFunction** - Execute table-valued functions with optional parameters
12. **ListProceduresAndFunctions** - List all stored procedures and functions
13. **DescribeProcedureOrFunction** - Get detailed metadata about procedures/functions

## Example Usage

### Creating and Executing a Stored Procedure
```sql
-- Create a procedure using CreateProcedure tool
CREATE PROCEDURE dbo.GetUsersByRole
    @Role NVARCHAR(50)
AS
BEGIN
    SELECT * FROM Users WHERE Role = @Role
END

-- Execute the procedure using ExecuteStoredProcedure tool
-- Parameters: {"@Role": "Admin"}
```

### Creating and Executing a Function
```sql
-- Create a table-valued function using CreateFunction tool
CREATE FUNCTION dbo.GetActiveUsers(@MinLoginDate DATE)
RETURNS TABLE
AS
RETURN
(
    SELECT * FROM Users 
    WHERE LastLogin >= @MinLoginDate 
    AND IsActive = 1
)

-- Execute the function using ExecuteFunction tool
-- Parameters: {"MinLoginDate": "2024-01-01"}
```

# Troubleshooting

1. If you get a "Task canceled" error using "Active Directory Default", try "Active Directory Interactive".
2. For stored procedures with output parameters, include them in the parameters dictionary.
3. Function execution requires the function to be table-valued for proper result return.