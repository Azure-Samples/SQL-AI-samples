
# Mssql SQL MCP Server (.NET 8)

This project is a .NET 8 console application implementing a Model Context Protocol (MCP) server for MSSQL Databases using the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).

## Features

- Provide connection string via environment variable `CONNECTION_STRING`.
- **MCP Tools Implemented**:
  - ListTables: List all tables in the database.
  - DescribeTable: Get schema/details for a table.
  - CreateTable: Create new tables.
  - DropTable: Drop existing tables.
  - InsertData: Insert data into tables.
  - ReadData: Read/query data from tables.
  - UpdateData: Update values in tables.
- **Logging**: Console logging using Microsoft.Extensions.Logging.
- **Unit Tests**: xUnit-based unit tests for all major components.

## Getting Started

### Prerequisites

- Access to a SQL Server or Azure SQL Database

### Setup

1. **Build ***

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

Save the file, start a new Chat, you'll see the "Tools" icon, it should list 7 MSSQL MCP tools.

# Troubleshooting

1. If you get a "Task canceled" error using "Active Directory Default", try "Active Directory Interactive".



