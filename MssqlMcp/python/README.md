# MSSQL MCP Server - Python Implementation

A Python implementation of the Model Context Protocol (MCP) server for Microsoft SQL Server database operations. This server provides tools for database management including table operations, data manipulation, and schema inspection.

## Features

This MCP server provides the following tools:

- **List Tables**: Lists all tables in the SQL Database
- **Describe Table**: Returns detailed table schema information including columns, indexes, constraints, and foreign keys
- **Create Table**: Creates new tables using CREATE TABLE SQL statements
- **Drop Table**: Drops tables using DROP TABLE SQL statements
- **Read Data**: Executes SELECT queries to read data from the database
- **Insert Data**: Inserts data using INSERT SQL statements
- **Update Data**: Updates data using UPDATE SQL statements

## Prerequisites

- Python 3.8 or higher
- Microsoft SQL Server (local or remote)
- ODBC Driver for SQL Server

### Installing ODBC Driver for SQL Server

#### Windows
The ODBC driver is typically pre-installed on Windows systems.

#### macOS
```bash
# Install using Homebrew
brew install microsoft/mssql-release/msodbcsql18 microsoft/mssql-release/mssql-tools18

# Or download from Microsoft's website
# https://docs.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server
```

#### Linux (Ubuntu/Debian)
```bash
# Import Microsoft repository key
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -

# Add Microsoft repository
curl https://packages.microsoft.com/config/ubuntu/20.04/prod.list | sudo tee /etc/apt/sources.list.d/msprod.list

# Update package list
sudo apt-get update

# Install ODBC driver
sudo apt-get install msodbcsql18
```

## Installation

1. Clone or download this repository
2. Navigate to the Python implementation directory:
   ```bash
   cd MssqlMcp/python
   ```

3. Create a virtual environment (recommended):
   ```bash
   python -m venv venv
   source venv/bin/activate  # On Windows: venv\Scripts\activate
   ```

4. Install dependencies:
   ```bash
   pip install -r requirements.txt
   ```

## Configuration

Set the connection string environment variable:

### Windows
```cmd
set CONNECTION_STRING=Server=.;Database=test;Trusted_Connection=yes;TrustServerCertificate=yes
```

### macOS/Linux
```bash
export CONNECTION_STRING="Server=.;Database=test;Trusted_Connection=yes;TrustServerCertificate=yes"
```

### Connection String Examples

#### Local SQL Server with Windows Authentication
```
Server=.;Database=test;Trusted_Connection=yes;TrustServerCertificate=yes
```

#### Local SQL Server with SQL Authentication
```
Server=.;Database=test;User Id=myuser;Password=mypassword;TrustServerCertificate=yes
```

#### Remote SQL Server
```
Server=myserver.database.windows.net;Database=mydatabase;User Id=myuser;Password=mypassword;Encrypt=yes;TrustServerCertificate=no
```

#### Azure SQL Database
```
Server=tcp:myserver.database.windows.net,1433;Database=mydatabase;User Id=myuser@myserver;Password=mypassword;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;
```

## Usage

### Running the Server

```bash
python main.py
```

The server will start and listen for MCP protocol messages via standard input/output.

### Using with MCP Clients

This server is designed to work with MCP-compatible clients. The server provides the following tools:

#### List Tables
```json
{
  "name": "list_tables",
  "arguments": {}
}
```

#### Describe Table
```json
{
  "name": "describe_table",
  "arguments": {
    "table_name": "dbo.Users"
  }
}
```

#### Create Table
```json
{
  "name": "create_table",
  "arguments": {
    "sql": "CREATE TABLE dbo.Users (Id INT PRIMARY KEY, Name NVARCHAR(100))"
  }
}
```

#### Drop Table
```json
{
  "name": "drop_table",
  "arguments": {
    "sql": "DROP TABLE dbo.Users"
  }
}
```

#### Read Data
```json
{
  "name": "read_data",
  "arguments": {
    "sql": "SELECT * FROM dbo.Users WHERE Id = 1"
  }
}
```

#### Insert Data
```json
{
  "name": "insert_data",
  "arguments": {
    "sql": "INSERT INTO dbo.Users (Id, Name) VALUES (1, 'John Doe')"
  }
}
```

#### Update Data
```json
{
  "name": "update_data",
  "arguments": {
    "sql": "UPDATE dbo.Users SET Name = 'Jane Doe' WHERE Id = 1"
  }
}
```

## Project Structure

```
python/
├── main.py                 # Entry point script
├── requirements.txt        # Python dependencies
├── README.md              # This file
└── src/
    └── mssql_mcp/
        ├── __init__.py
        ├── server.py              # Main MCP server implementation
        ├── db_operation_result.py # Result class for database operations
        ├── sql_connection_factory.py # Database connection management
        └── tools/                 # Database operation tools
            ├── __init__.py
            ├── list_tables.py
            ├── describe_table.py
            ├── create_table.py
            ├── drop_table.py
            ├── read_data.py
            ├── insert_data.py
            └── update_data.py
```

## Error Handling

The server provides comprehensive error handling:

- Connection errors are logged and returned to the client
- SQL execution errors are captured and returned with details
- Invalid tool requests are handled gracefully
- All database connections are properly closed after use

## Security Considerations

- Always use parameterized queries when possible (implemented in describe_table)
- Validate SQL statements before execution
- Use appropriate authentication methods for your environment
- Consider using read-only connections for query operations
- Regularly update dependencies for security patches

## Troubleshooting

### Common Issues

1. **Connection String Not Set**
   - Ensure the `CONNECTION_STRING` environment variable is properly set
   - Verify the connection string format is correct

2. **ODBC Driver Not Found**
   - Install the Microsoft ODBC Driver for SQL Server
   - Verify the driver is properly installed and accessible

3. **Authentication Failures**
   - Check username/password for SQL authentication
   - Verify Windows authentication settings
   - Ensure the user has appropriate database permissions

4. **SSL/TLS Certificate Issues**
   - Use `TrustServerCertificate=yes` for development environments
   - Ensure proper certificate configuration for production

### Logging

The server logs important events and errors to stderr. Check the logs for detailed error information when troubleshooting issues.

## Contributing

This is a Microsoft-provided sample implementation. For issues or contributions, please refer to the main repository guidelines.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Related Projects

- [.NET Implementation](../dotnet/) - C# implementation of the same MCP server
- [Node.js Implementation](../Node/) - TypeScript/JavaScript implementation
- [Model Context Protocol](https://github.com/modelcontextprotocol) - Official MCP specification and tools
