# MSSQL Database MCP  Server

<div align="center">
  <img src="./src/img/logo.png" alt="MSSQL Database MCP server logo" width="400"/>
</div>

## What is this? 🤔

This is a server that lets your LLMs (like Claude) talk directly to your MSSQL Database data! Think of it as a friendly translator that sits between your AI assistant and your database, making sure they can chat securely and efficiently.

### Quick Example
```text
You: "Show me all customers from New York"
Claude: *queries your MSSQL Database database and gives you the answer in plain English*
```


## How Does It Work? 🛠️

This server leverages the Model Context Protocol (MCP), a versatile framework that acts as a universal translator between AI models and databases. Although MCP is built to support any AI model, it is currently accessible as a developer preview in Claude Desktop.

Here's all you need to do:
1. Set up project (see below)
2. Add your project details to Claude Desktop's config file
3. Start chatting with your MSSQL Database data naturally!

### What Can It Do? 📊

- Run MSSQL Database queries by just asking questions in plain English
- Create, read, update, and delete data
- Manage database schema (tables, indexes)
- Secure connection handling
- Real-time data interaction

## Quick Start 🚀

### Prerequisites
- Node.js 14 or higher
- Claude Desktop 

### Set up project

- Obtain MSSQL Database server_name, database_name. Create an '.env' file and fill in details with the obtained values

```
SERVER_NAME='<INSERT>'
DATABASE_NAME='<INSERT>'
```

### Getting Started

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

3. **Start the Server**  
   Navigate to the `dist` folder and start the server:  
   ```bash
   npm start
   ```

4. **Confirmation Message**  
   You should see the following message:  
   ```
   MSSQL Database Server running on stdio
   ```

### Add your project details to Claude Destkop's config file, and set parameters accordingly

Open Claude Desktop and Navigate to File -> Settings -> Developer -> Edit Config and open the `claude_desktop_config` file and replace with the values below,

```json
{
    "mcpServers": {
      "mssql": {
        "command": "node",
        "args": [ "C:/repos/MCP/dist/index.js" ],
        "env": {
          "SERVER_NAME": "",
          "DATABASE_NAME": "",
          "READONLY": "" // set to "true" to restrict to read-only operations
        }
      }
    }
  }

```

You should now have successfully configured the MCP server for MSSQL Database with Claude Desktop. This setup allows you to seamlessly interact with MSSQL Database through the MCP server.
