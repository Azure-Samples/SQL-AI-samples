#!/usr/bin/env node

// External imports
import * as dotenv from "dotenv";
import sql from "mssql";
import * as http from "http";
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { SSEServerTransport } from "@modelcontextprotocol/sdk/server/sse.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";

// Internal imports
import { UpdateDataTool } from "./tools/UpdateDataTool.js";
import { InsertDataTool } from "./tools/InsertDataTool.js";
import { ReadDataTool } from "./tools/ReadDataTool.js";
import { CreateTableTool } from "./tools/CreateTableTool.js";
import { CreateIndexTool } from "./tools/CreateIndexTool.js";
import { ListTableTool } from "./tools/ListTableTool.js";
import { DropTableTool } from "./tools/DropTableTool.js";
import { DefaultAzureCredential, InteractiveBrowserCredential } from "@azure/identity";
import { DescribeTableTool } from "./tools/DescribeTableTool.js";

// MSSQL Database connection configuration
// const credential = new DefaultAzureCredential();
dotenv.config();

// Globals for connection and token reuse
let globalSqlPool: sql.ConnectionPool | null = null;
let globalAccessToken: string | null = null;
let globalTokenExpiresOn: Date | null = null;

// Function to create SQL config with fresh access token, returns token and expiry
export async function createSqlConfig(): Promise<{ config: sql.config, token: string, expiresOn: Date }> {
  const trustServerCertificate = process.env.TRUST_SERVER_CERTIFICATE?.toLowerCase() === 'true';
  const connectionTimeout = process.env.CONNECTION_TIMEOUT ? parseInt(process.env.CONNECTION_TIMEOUT, 10) : 30;

  // Base configuration shared by both auth methods
  const baseConfig = {
    server: process.env.SERVER_NAME!,
    database: process.env.DATABASE_NAME!,
    options: {
      encrypt: true,
      trustServerCertificate
    },
    connectionTimeout: connectionTimeout * 1000, 
  };

  // ---------------------------------------------------------
  // PATH A: Standard SQL Authentication (via .env)
  // ---------------------------------------------------------
  if (process.env.SQL_USER && process.env.SQL_PASSWORD) {
    return {
      config: {
        ...baseConfig,
        user: process.env.SQL_USER,
        password: process.env.SQL_PASSWORD,
      },
      token: '',
      expiresOn: new Date(Date.now() + 30 * 60 * 1000)
    };
  }

  // ---------------------------------------------------------
  // PATH B: Azure Active Directory Authentication (Browser Token)
  // ---------------------------------------------------------
  const credential = new InteractiveBrowserCredential({
    redirectUri: 'http://localhost'
    // disableAutomaticAuthentication : true
  });
  
  const accessToken = await credential.getToken('https://database.windows.net/.default');

  return {
    config: {
      ...baseConfig,
      authentication: {
        type: 'azure-active-directory-access-token',
        options: {
          token: accessToken?.token!,
        },
      },
    },
    token: accessToken?.token!,
    expiresOn: accessToken?.expiresOnTimestamp ? new Date(accessToken.expiresOnTimestamp) : new Date(Date.now() + 30 * 60 * 1000)
  };
}

const updateDataTool = new UpdateDataTool();
const insertDataTool = new InsertDataTool();
const readDataTool = new ReadDataTool();
const createTableTool = new CreateTableTool();
const createIndexTool = new CreateIndexTool();
const listTableTool = new ListTableTool();
const dropTableTool = new DropTableTool();
const describeTableTool = new DescribeTableTool();

const server = new Server(
  {
    name: "mssql-mcp-server",
    version: "0.1.0",
  },
  {
    capabilities: {
      tools: {},
    },
  },
);

// Read READONLY env variable
const isReadOnly = process.env.READONLY === "true";

// Request handlers

server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: isReadOnly
    ? [listTableTool, readDataTool, describeTableTool] // todo: add searchDataTool to the list of tools available in readonly mode once implemented
    : [insertDataTool, readDataTool, describeTableTool, updateDataTool, createTableTool, createIndexTool, dropTableTool, listTableTool], // add all new tools here
}));

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;
  try {
    let result;
    switch (name) {
      case insertDataTool.name:
        result = await insertDataTool.run(args);
        break;
      case readDataTool.name:
        result = await readDataTool.run(args);
        break;
      case updateDataTool.name:
        result = await updateDataTool.run(args);
        break;
      case createTableTool.name:
        result = await createTableTool.run(args);
        break;
      case createIndexTool.name:
        result = await createIndexTool.run(args);
        break;
      case listTableTool.name:
        result = await listTableTool.run(args);
        break;
      case dropTableTool.name:
        result = await dropTableTool.run(args);
        break;
      case describeTableTool.name:
        if (!args || typeof args.tableName !== "string") {
          return {
            content: [{ type: "text", text: `Missing or invalid 'tableName' argument for describe_table tool.` }],
            isError: true,
          };
        }
        result = await describeTableTool.run(args as { tableName: string });
        break;
      default:
        return {
          content: [{ type: "text", text: `Unknown tool: ${name}` }],
          isError: true,
        };
    }
    return {
      content: [{ type: "text", text: JSON.stringify(result, null, 2) }],
    };
  } catch (error) {
    return {
      content: [{ type: "text", text: `Error occurred: ${error}` }],
      isError: true,
    };
  }
});

// Server startup
async function runServer() {
  try {
    const transport = new StdioServerTransport();
    await server.connect(transport);
  } catch (error) {
    console.error("Fatal error running server:", error);
    process.exit(1);
  }
}

// Connect to SQL only when handling a request

async function ensureSqlConnection() {
  // If we have a pool and it's connected, and the token is still valid, reuse it
  if (
    globalSqlPool &&
    globalSqlPool.connected &&
    globalAccessToken &&
    globalTokenExpiresOn &&
    globalTokenExpiresOn > new Date(Date.now() + 2 * 60 * 1000) // 2 min buffer
  ) {
    return;
  }

  // Otherwise, get a new token and reconnect
  const { config, token, expiresOn } = await createSqlConfig();
  globalAccessToken = token;
  globalTokenExpiresOn = expiresOn;

  // Close old pool if exists
  if (globalSqlPool && globalSqlPool.connected) {
    await globalSqlPool.close();
  }

  globalSqlPool = await sql.connect(config);
}

// Patch all tool handlers to ensure SQL connection before running
function wrapToolRun(tool: { run: (...args: any[]) => Promise<any> }) {
  const originalRun = tool.run.bind(tool);
  tool.run = async function (...args: any[]) {
    await ensureSqlConnection();
    return originalRun(...args);
  };
}

[insertDataTool, readDataTool, updateDataTool, createTableTool, createIndexTool, dropTableTool, listTableTool, describeTableTool].forEach(wrapToolRun);

// ==========================================
// Transports & Server Startup
// ==========================================

// HTTP / Server-Sent Events Transport
async function runHttpServer(port: number) {
  
  // Map to store active client sessions
  const activeTransports = new Map<string, SSEServerTransport>();

  const httpServer = http.createServer(async (req, res) => {
    // Parse the URL to get the path and query parameters
    const parsedUrl = new URL(req.url || "/", `http://${req.headers.host}`);

    // Add basic CORS headers for web clients
    res.setHeader("Access-Control-Allow-Origin", "*");
    res.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
    res.setHeader("Access-Control-Allow-Headers", "Content-Type");

    // Handle preflight requests
    if (req.method === "OPTIONS") {
      res.writeHead(200);
      res.end();
      return;
    }

    // ---------------------------------------------------------
    // SSE Connection Endpoint (Handshake & Stream Start)
    // ---------------------------------------------------------
    if (req.method === "GET" && parsedUrl.pathname === "/sse") {
      console.error("New client connecting...");
      
      const transport = new SSEServerTransport("/messages", res as any);
      
      // Connect this specific transport to the server instance
      await server.connect(transport);

      // Store the transport session so we can route POST messages to it later
      activeTransports.set(transport.sessionId, transport);
      console.error(`Client connected. Session ID: ${transport.sessionId}`);

      // Crucial: Clean up memory when the client disconnects
      res.on("close", () => {
        console.error(`Client disconnected. Cleaning up Session ID: ${transport.sessionId}`);
        activeTransports.delete(transport.sessionId);
      });
      return;
    } 
    
    // ---------------------------------------------------------
    // Message Posting Endpoint (Receiving Client Tool Calls)
    // ---------------------------------------------------------
    if (req.method === "POST" && parsedUrl.pathname === "/messages") {
      const sessionId = parsedUrl.searchParams.get("sessionId");

      if (!sessionId) {
        res.writeHead(400, { "Content-Type": "application/json" });
        res.end(JSON.stringify({ error: "Missing sessionId in query parameters." }));
        return;
      }

      const transport = activeTransports.get(sessionId);

      if (transport) {
        // Native Node http handles the stream internally in the SDK, 
        // no need to parse req.body like in Express
        await transport.handlePostMessage(req as any, res as any);
      } else {
        res.writeHead(404, { "Content-Type": "application/json" });
        res.end(JSON.stringify({ error: "Session not found or expired. Reconnect to /sse." }));
      }
      return;
    }

    // 404 Fallback
    res.writeHead(404);
    res.end("Not Found");
  });

  httpServer.listen(port, () => {
    console.error(`Multi-Client MSSQL MCP Server running on HTTP/SSE.`);
    console.error(`Connect to SSE stream at: http://localhost:${port}/sse`);
    console.error(`Send POST messages to:   http://localhost:${port}/messages?sessionId=...`);
  });
}

// ==========================================
// Execution Logic
// ==========================================

// Parse command line arguments
const args = process.argv.slice(2);
const isSSE = args.includes("--sse");
const portArgIndex = args.indexOf("--port");
const port = portArgIndex !== -1 ? parseInt(args[portArgIndex + 1], 10) : 3000;

if (isSSE) {
  runHttpServer(port).catch((error) => {
    console.error("Fatal error running HTTP server:", error);
    process.exit(1);
  });
} else {
  runServer().catch((error) => {
    console.error("Fatal error running stdio server:", error);
    process.exit(1);
  });
}