#!/usr/bin/env node

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";

import { loadConfig } from "./config.js";
import { ConnectionManager } from "./ConnectionManager.js";
import { UpdateDataTool } from "./tools/UpdateDataTool.js";
import { InsertDataTool } from "./tools/InsertDataTool.js";
import { ReadDataTool } from "./tools/ReadDataTool.js";
import { CreateTableTool } from "./tools/CreateTableTool.js";
import { CreateIndexTool } from "./tools/CreateIndexTool.js";
import { ListTableTool } from "./tools/ListTableTool.js";
import { DropTableTool } from "./tools/DropTableTool.js";
import { DescribeTableTool } from "./tools/DescribeTableTool.js";
import { ListDatabasesTool } from "./tools/ListDatabasesTool.js";

const config = loadConfig();
const connectionManager = new ConnectionManager(config);

const listDatabasesTool = new ListDatabasesTool(connectionManager);
const updateDataTool = new UpdateDataTool(connectionManager);
const insertDataTool = new InsertDataTool(connectionManager);
const readDataTool = new ReadDataTool(connectionManager);
const createTableTool = new CreateTableTool(connectionManager);
const createIndexTool = new CreateIndexTool(connectionManager);
const listTableTool = new ListTableTool(connectionManager);
const dropTableTool = new DropTableTool(connectionManager);
const describeTableTool = new DescribeTableTool(connectionManager);

const allTools = [
  listDatabasesTool,
  insertDataTool,
  readDataTool,
  describeTableTool,
  updateDataTool,
  createTableTool,
  createIndexTool,
  dropTableTool,
  listTableTool,
];

const readOnlyTools = [listDatabasesTool, listTableTool, readDataTool, describeTableTool];

const server = new Server(
  { name: "mssql-mcp-server", version: "0.2.0" },
  { capabilities: { tools: {} } },
);

server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: config.readonly ? readOnlyTools : allTools,
}));

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;
  const tool = allTools.find(t => t.name === name);
  if (!tool) {
    return {
      content: [{ type: "text", text: `Unknown tool: ${name}` }],
      isError: true,
    };
  }
  try {
    const result = await tool.run(args);
    return {
      content: [{ type: "text", text: JSON.stringify(result, null, 2) }],
    };
  } catch (error) {
    return {
      content: [{ type: "text", text: `Error: ${error}` }],
      isError: true,
    };
  }
});

async function runServer() {
  try {
    const transport = new StdioServerTransport();
    await server.connect(transport);
  } catch (error) {
    console.error("Fatal error running server:", error);
    process.exit(1);
  }
}

runServer().catch((error) => {
  console.error("Fatal error running server:", error);
  process.exit(1);
});
