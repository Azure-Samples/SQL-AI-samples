#!/usr/bin/env node

// External imports
import * as dotenv from "dotenv";
import sql from "mssql";
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";

dotenv.config();

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
import { SearchSchemaTool } from "./tools/SearchSchemaTool.js";
import { ProfileTableTool } from "./tools/ProfileTableTool.js";

// MSSQL Database connection configuration
// const credential = new DefaultAzureCredential();

// Globals for connection and token reuse
let globalSqlPool: sql.ConnectionPool | null = null;
let globalAccessToken: string | null = null;
let globalTokenExpiresOn: Date | null = null;

// Function to create SQL config with fresh access token, returns token and expiry
export async function createSqlConfig(): Promise<{ config: sql.config, token: string | null, expiresOn: Date | null }> {
  const server = process.env.SERVER_NAME;
  const database = process.env.DATABASE_NAME;
  if (!server || !database) {
    throw new Error("SERVER_NAME and DATABASE_NAME environment variables are required.");
  }

  const trustServerCertificate = process.env.TRUST_SERVER_CERTIFICATE?.toLowerCase() === 'true';
  const connectionTimeout = process.env.CONNECTION_TIMEOUT ? parseInt(process.env.CONNECTION_TIMEOUT, 10) : 30;
  const sqlPort = process.env.SQL_PORT ? parseInt(process.env.SQL_PORT, 10) : undefined;
  const authMode = (process.env.SQL_AUTH_MODE ?? 'aad').toLowerCase();

  if (authMode === 'sql') {
    const sqlUsername = process.env.SQL_USERNAME;
    const sqlPassword = process.env.SQL_PASSWORD;

    if (!sqlUsername || !sqlPassword) {
      throw new Error("SQL_AUTH_MODE=sql requires SQL_USERNAME and SQL_PASSWORD to be set.");
    }

    return {
      config: {
        server,
        database,
        port: sqlPort,
        user: sqlUsername,
        password: sqlPassword,
        options: {
          encrypt: false,
          trustServerCertificate,
        },
        connectionTimeout: connectionTimeout * 1000,
      },
      token: null,
      expiresOn: null,
    };
  }

  if (authMode === 'windows') {
    const sqlUsername = process.env.SQL_USERNAME;
    const sqlPassword = process.env.SQL_PASSWORD;
    const sqlDomain = process.env.SQL_DOMAIN ?? "";

    if (!sqlUsername || !sqlPassword) {
      throw new Error("SQL_AUTH_MODE=windows requires SQL_USERNAME and SQL_PASSWORD to be set.");
    }

    return {
      config: {
        server,
        database,
        port: sqlPort,
        options: {
          encrypt: false,
          trustServerCertificate,
        },
        authentication: {
          type: 'ntlm',
          options: {
            userName: sqlUsername,
            password: sqlPassword,
            domain: sqlDomain,
          },
        },
        connectionTimeout: connectionTimeout * 1000,
      },
      token: null,
      expiresOn: null,
    };
  }

  const credential = new InteractiveBrowserCredential({
    redirectUri: 'http://localhost'
    // disableAutomaticAuthentication : true
  });
  const accessToken = await credential.getToken('https://database.windows.net/.default');

  if (!accessToken?.token) {
    throw new Error("Failed to acquire Azure AD access token for SQL connection.");
  }

  return {
    config: {
      server,
      database,
      port: sqlPort,
      options: {
        encrypt: true,
        trustServerCertificate
      },
      authentication: {
        type: 'azure-active-directory-access-token',
        options: {
          token: accessToken.token,
        },
      },
      connectionTimeout: connectionTimeout * 1000, // convert seconds to milliseconds
    },
    token: accessToken.token,
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
const searchSchemaTool = new SearchSchemaTool();
const profileTableTool = new ProfileTableTool();

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
    ? [listTableTool, readDataTool, describeTableTool, searchSchemaTool, profileTableTool]
    : [insertDataTool, readDataTool, describeTableTool, updateDataTool, createTableTool, createIndexTool, dropTableTool, listTableTool, searchSchemaTool, profileTableTool],
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
      case searchSchemaTool.name:
        result = await searchSchemaTool.run(args ?? {});
        break;
      case profileTableTool.name:
        if (!args || typeof args.tableName !== "string") {
          return {
            content: [{ type: "text", text: `Missing or invalid 'tableName' argument for profile_table tool.` }],
            isError: true,
          };
        }
        result = await profileTableTool.run(args as any);
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

runServer().catch((error) => {
  console.error("Fatal error running server:", error);
  process.exit(1);
});

// Connect to SQL only when handling a request

async function ensureSqlConnection() {
  // If we have a pool and it's connected, and the token is still valid, reuse it
  if (
    globalSqlPool &&
    globalSqlPool.connected &&
    (!globalTokenExpiresOn || globalTokenExpiresOn > new Date(Date.now() + 2 * 60 * 1000)) // 2 min buffer
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

[insertDataTool, readDataTool, updateDataTool, createTableTool, createIndexTool, dropTableTool, listTableTool, describeTableTool, searchSchemaTool, profileTableTool].forEach(wrapToolRun);