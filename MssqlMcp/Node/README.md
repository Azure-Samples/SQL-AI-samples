# MSSQL Database MCP  Server

<div align="center">
  <img src="./src/img/logo.png" alt="MSSQL Database MCP server logo" width="400"/>
</div>

> ⚠️ **EXPERIMENTAL USE ONLY** - This MCP Server is provided as an example for educational and experimental purposes only. It is NOT intended for production use. Please use appropriate security measures and thoroughly test before considering any kind of deployment.

## What is this? 🤔

This is a server that lets your LLMs (like Claude) talk directly to your MSSQL Database data! Think of it as a friendly translator that sits between your AI assistant and your database, making sure they can chat securely and efficiently.

### Quick Example
```text
You: "Show me all customers from New York"
Claude: *queries your MSSQL Database database and gives you the answer in plain English*
```

## How Does It Work? 🛠️

This server leverages the Model Context Protocol (MCP), a versatile framework that acts as a universal translator between AI models and databases. It supports multiple AI assistants including Claude Desktop and VS Code Agent.

### What Can It Do? 📊

- Run MSSQL Database queries by just asking questions in plain English
- Create, read, update, and delete data
- Manage database schema (tables, indexes)
- Secure connection handling
- Real-time data interaction

## Quick Start 🚀

### Prerequisites
- Node.js 14 or higher
- Claude Desktop or VS Code with Agent extension

### Set up project

1. **Install Dependencies**  
   Run the following command in the root folder to install all necessary dependencies:  
   ```bash
   npm install
   ```

2. **Build the Project**  
   Compile the project by running:  
   ```bash
   npm run build
   ```

## Configuration Setup

### Option 1: VS Code Agent Setup

1. **Install VS Code Agent Extension**
   - Open VS Code
   - Go to Extensions (Ctrl+Shift+X)
   - Search for "Agent" and install the official Agent extension

2. **Create MCP Configuration File**
   - Create a `.vscode/mcp.json` file in your workspace
   - Add the following configuration:

   ```json
   {
     "servers": {
       "mssql-nodejs": {
          "type": "stdio",
          "command": "node",
          "args": ["q:\\Repos\\SQL-AI-samples\\MssqlMcp\\Node\\dist\\index.js"],
          "env": {
            "SERVER_NAME": "your-server-name.database.windows.net",
            "DATABASE_NAME": "your-database-name",
            "READONLY": "false"
          }
        }
      }
   }
   ```

3. **Alternative: User Settings Configuration**
   - Open VS Code Settings (Ctrl+,)
   - Search for "mcp"
   - Click "Edit in settings.json"
   - Add the following configuration:

  ```json
   {
    "mcp": {
        "servers": {
            "mssql": {
                "command": "node",
                "args": ["C:/path/to/your/Node/dist/index.js"],
                "env": {
                "SERVER_NAME": "your-server-name.database.windows.net",
                "DATABASE_NAME": "your-database-name",
                "AUTH_METHOD": "azure-ad",
                "READONLY": "false"
                }
            }
        }
    }
  }
  }
  }

4. **Restart VS Code**
   - Close and reopen VS Code for the changes to take effect

5. **Verify MCP Server**
   - Open Command Palette (Ctrl+Shift+P)
   - Run "MCP: List Servers" to verify your server is configured
   - You should see "mssql" in the list of available servers

### Option 2: Claude Desktop Setup

1. **Open Claude Desktop Settings**
   - Navigate to File → Settings → Developer → Edit Config
   - Open the `claude_desktop_config` file

2. **Add MCP Server Configuration**
   Replace the content with the configuration below, updating the path and credentials:

   ```json
   {
     "mcpServers": {
       "mssql": {
         "command": "node",
         "args": ["C:/path/to/your/Node/dist/index.js"],
         "env": {
           "SERVER_NAME": "your-server-name.database.windows.net",
           "DATABASE_NAME": "your-database-name",
           "AUTH_METHOD": "azure-ad",
           "READONLY": "false"
         }
       }
     }
   }
   }
   }

3. **Restart Claude Desktop**
   - Close and reopen Claude Desktop for the changes to take effect

## Authentication Configuration

The MCP server supports multiple authentication methods to connect to your MSSQL database. Choose the method that best fits your environment and security requirements.

### Authentication Methods

#### 1. Azure Active Directory (Default)
**Best for:** Azure SQL Database, managed identities, modern security requirements

This method uses Azure AD authentication with interactive browser login. No username/password needed in configuration.

```json
{
  "env": {
    "SERVER_NAME": "your-server.database.windows.net",
    "DATABASE_NAME": "your-database-name",
    "AUTH_METHOD": "azure-ad"
  }
}
```

**Required Environment Variables:**
- `AUTH_METHOD`: Set to `"azure-ad"` or `"azuread"`
- `SERVER_NAME`: Your Azure SQL server name
- `DATABASE_NAME`: Your database name

**Notes:**
- Uses interactive browser authentication
- Automatically handles token refresh
- Requires Azure AD permissions for the database
- Most secure option for Azure SQL Database

#### 2. SQL Server Authentication
**Best for:** Traditional SQL Server instances, local development

Uses SQL Server's built-in authentication with username and password.

```json
{
  "env": {
    "SERVER_NAME": "your-server.database.windows.net",
    "DATABASE_NAME": "your-database-name",
    "AUTH_METHOD": "sql",
    "SQL_USERNAME": "your-sql-username",
    "SQL_PASSWORD": "your-sql-password"
  }
}
```

**Required Environment Variables:**
- `AUTH_METHOD`: Set to `"sql"` or `"sqlserver"`
- `SERVER_NAME`: Your SQL Server instance
- `DATABASE_NAME`: Your database name
- `SQL_USERNAME`: SQL Server username
- `SQL_PASSWORD`: SQL Server password

#### 3. Windows Authentication (NTLM)
**Best for:** On-premises SQL Server with Windows domain authentication

Uses Windows credentials for authentication.

```json
{
  "env": {
    "SERVER_NAME": "your-server-instance",
    "DATABASE_NAME": "your-database-name",
    "AUTH_METHOD": "windows",
    "DOMAIN": "your-domain",
    "USERNAME": "your-windows-username",
    "PASSWORD": "your-windows-password"
  }
}
```

**Required Environment Variables:**
- `AUTH_METHOD`: Set to `"windows"` or `"ntlm"`
- `SERVER_NAME`: Your SQL Server instance
- `DATABASE_NAME`: Your database name
- `DOMAIN`: Windows domain (optional, can be empty)
- `USERNAME`: Windows username
- `PASSWORD`: Windows password

### Configuration Parameters

#### Common Parameters (All Authentication Methods)
- **SERVER_NAME**: Your MSSQL Database server name (e.g., `my-server.database.windows.net`)
- **DATABASE_NAME**: Your database name
- **AUTH_METHOD**: Authentication method to use (`"azure-ad"`, `"sql"`, or `"windows"`). Defaults to `"azure-ad"` if not specified.
- **READONLY**: Set to `"true"` to restrict to read-only operations, `"false"` for full access
- **Path**: Update the path in `args` to point to your actual project location.
- **CONNECTION_TIMEOUT**: (Optional) Connection timeout in seconds. Defaults to `30` if not set.
- **TRUST_SERVER_CERTIFICATE**: (Optional) Set to `"true"` to trust self-signed server certificates (useful for development or when connecting to servers with self-signed certs). Defaults to `"false"`.

#### Authentication-Specific Parameters
- **SQL_USERNAME** & **SQL_PASSWORD**: Required for SQL Server authentication (`AUTH_METHOD="sql"`)
- **USERNAME**, **PASSWORD** & **DOMAIN**: Required for Windows authentication (`AUTH_METHOD="windows"`)
- **No additional parameters needed**: For Azure AD authentication (`AUTH_METHOD="azure-ad"`)

## Sample Configurations

You can find sample configuration files in the `src/samples/` folder:
- `claude_desktop_config.json` - For Claude Desktop
- `vscode_agent_config.json` - For VS Code Agent

## Usage Examples

Once configured, you can interact with your database using natural language:

- "Show me all users from New York"
- "Create a new table called products with columns for id, name, and price"
- "Update all pending orders to completed status"
- "List all tables in the database"

## Security Notes

- The server requires a WHERE clause for read operations to prevent accidental full table scans
- Update operations require explicit WHERE clauses for security
- Set `READONLY: "true"` in environments if you only need read access

You should now have successfully configured the MCP server for MSSQL Database with your preferred AI assistant. This setup allows you to seamlessly interact with MSSQL Database through natural language queries!
