# Azure SQL DB MCP  Server

<div align="center">
  <img src="./src/img/logo.png" alt="Azure SQL DB MCP server logo" width="400"/>
</div>

## What is this? 🤔

This is a server that lets your LLMs (like Claude) talk directly to your Azure SQL DB data! Think of it as a friendly translator that sits between your AI assistant and your database, making sure they can chat securely and efficiently.

### Quick Example
```text
You: "What were our top 10 customers last month?"
Claude: *queries your Azure SQL DB database and gives you the answer in plain English*
```


## How Does It Work? 🛠️

This server leverages the Model Context Protocol (MCP), a versatile framework that acts as a universal translator between AI models and databases. Although MCP is built to support any AI model, it is currently accessible as a developer preview in Claude Desktop.

Here's all you need to do:
1. Set up project (see below)
2. Add your project details to Claude Desktop's config file
3. Start chatting with your Azure SQL DB data naturally!

### What Can It Do? 📊

- Run Azure SQL DB queries by just asking questions in plain English

## Quick Start 🚀

### Prerequisites
- Node.js 14 or higher
- Claude Desktop 

### Set up project

- Obtain Azure SQL DB server_name, database_name. Create an '.env' file and fill in details with the obtained values

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
   Azure SQL DB Server running on stdio
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

You should now have successfully configured the MCP server for Azure SQL DB with Claude Desktop. This setup allows you to seamlessly interact with Azure SQL DB through the MCP server.
