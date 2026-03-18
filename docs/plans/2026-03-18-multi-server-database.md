# Multi-Server / Multi-Database Support — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Allow the MCP agent to dynamically discover databases across multiple MSSQL servers and target any database by name, without knowing which server hosts it.

**Architecture:** Replace the single global connection pool with a config-driven registry of named servers. A `ConnectionManager` resolves database names to servers via a cached `sys.databases` scan, maintains a pool-per-(server,database) pair, and tracks the "current" database for convenience. Each tool gains an optional `database` parameter. A new `list_databases` tool exposes available databases with parsed naming metadata.

**Tech Stack:** TypeScript, mssql (existing), Node.js, MCP SDK (existing)

---

### Task 1: Config file loader

**Files:**
- Create: `MssqlMcp/Node/src/config.ts`
- Create: `MssqlMcp/Node/mcp-config.sample.json`
- Test: `MssqlMcp/Node/src/__tests__/config.test.ts`

**Step 1: Write the failing test**

```typescript
// MssqlMcp/Node/src/__tests__/config.test.ts
import { describe, it, expect } from "vitest";
import { loadConfig, type McpConfig } from "../config.js";
import { writeFileSync, mkdirSync, rmSync } from "fs";
import { join } from "path";
import { tmpdir } from "os";

describe("loadConfig", () => {
  const tmpDir = join(tmpdir(), "mcp-config-test-" + Date.now());

  beforeEach(() => mkdirSync(tmpDir, { recursive: true }));
  afterEach(() => rmSync(tmpDir, { recursive: true, force: true }));

  it("loads a valid config file", () => {
    const configPath = join(tmpDir, "config.json");
    writeFileSync(configPath, JSON.stringify({
      readonly: false,
      connectionTimeout: 15,
      servers: [
        { name: "SQL2019", host: "localhost", port: 50075, user: "sa", password: "pwd" }
      ]
    }));

    const config = loadConfig(configPath);
    expect(config.servers).toHaveLength(1);
    expect(config.servers[0].name).toBe("SQL2019");
    expect(config.servers[0].port).toBe(50075);
    expect(config.readonly).toBe(false);
    expect(config.connectionTimeout).toBe(15);
  });

  it("supports instance name instead of port", () => {
    const configPath = join(tmpDir, "config.json");
    writeFileSync(configPath, JSON.stringify({
      servers: [
        { name: "SQLPROD", host: "prodserver", instance: "SQLEXPRESS", user: "sa", password: "pwd" }
      ]
    }));

    const config = loadConfig(configPath);
    expect(config.servers[0].instance).toBe("SQLEXPRESS");
    expect(config.servers[0].port).toBeUndefined();
  });

  it("throws on missing servers array", () => {
    const configPath = join(tmpDir, "config.json");
    writeFileSync(configPath, JSON.stringify({ readonly: true }));
    expect(() => loadConfig(configPath)).toThrow();
  });

  it("throws on server missing required fields", () => {
    const configPath = join(tmpDir, "config.json");
    writeFileSync(configPath, JSON.stringify({
      servers: [{ name: "SQL2019" }]
    }));
    expect(() => loadConfig(configPath)).toThrow();
  });

  it("applies defaults for optional fields", () => {
    const configPath = join(tmpDir, "config.json");
    writeFileSync(configPath, JSON.stringify({
      servers: [
        { name: "SQL2019", host: "localhost", port: 50075, user: "sa", password: "pwd" }
      ]
    }));

    const config = loadConfig(configPath);
    expect(config.readonly).toBe(false);
    expect(config.connectionTimeout).toBe(30);
  });
});
```

**Step 2: Install vitest and run the test to verify it fails**

Run:
```bash
cd MssqlMcp/Node && npm install --save-dev vitest
```

Add to `package.json` scripts: `"test": "vitest run"`.

Run:
```bash
cd MssqlMcp/Node && npx vitest run src/__tests__/config.test.ts
```
Expected: FAIL — `../config.js` does not exist.

**Step 3: Write the implementation**

```typescript
// MssqlMcp/Node/src/config.ts
import { readFileSync } from "fs";
import { resolve } from "path";
import { homedir } from "os";

export interface ServerConfig {
  name: string;
  host: string;
  port?: number;
  instance?: string;
  user: string;
  password: string;
}

export interface McpConfig {
  readonly: boolean;
  connectionTimeout: number;
  servers: ServerConfig[];
}

const DEFAULT_CONFIG_PATH = resolve(homedir(), ".mssql-mcp", "config.json");

export function loadConfig(configPath?: string): McpConfig {
  const filePath = configPath
    ?? process.env.MSSQL_MCP_CONFIG
    ?? DEFAULT_CONFIG_PATH;

  let raw: string;
  try {
    raw = readFileSync(filePath, "utf-8");
  } catch (err) {
    throw new Error(`Cannot read config file at ${filePath}: ${err}`);
  }

  const json = JSON.parse(raw);

  if (!Array.isArray(json.servers) || json.servers.length === 0) {
    throw new Error("Config must contain a non-empty 'servers' array");
  }

  const servers: ServerConfig[] = json.servers.map((s: any, i: number) => {
    if (!s.name || !s.host || !s.user || !s.password) {
      throw new Error(`Server at index ${i} is missing required fields (name, host, user, password)`);
    }
    if (!s.port && !s.instance) {
      throw new Error(`Server '${s.name}' must have either 'port' or 'instance'`);
    }
    return {
      name: s.name,
      host: s.host,
      port: s.port,
      instance: s.instance,
      user: s.user,
      password: s.password,
    };
  });

  return {
    readonly: json.readonly ?? false,
    connectionTimeout: json.connectionTimeout ?? 30,
    servers,
  };
}
```

**Step 4: Run the test to verify it passes**

Run: `cd MssqlMcp/Node && npx vitest run src/__tests__/config.test.ts`
Expected: all 5 tests PASS.

**Step 5: Create sample config file**

```json
// MssqlMcp/Node/mcp-config.sample.json
{
  "readonly": false,
  "connectionTimeout": 30,
  "servers": [
    {
      "name": "SQL2019",
      "host": "localhost",
      "port": 50075,
      "user": "sa",
      "password": "your-password"
    },
    {
      "name": "SQL2017",
      "host": "localhost",
      "port": 49990,
      "user": "sa",
      "password": "your-password"
    },
    {
      "name": "SQLPROD",
      "host": "prodserver",
      "instance": "SQLEXPRESS",
      "user": "sa",
      "password": "your-password"
    }
  ]
}
```

**Step 6: Commit**

```bash
git add MssqlMcp/Node/src/config.ts MssqlMcp/Node/src/__tests__/config.test.ts MssqlMcp/Node/mcp-config.sample.json
git commit -m "feat: add config file loader for multi-server support"
```

---

### Task 2: ConnectionManager — pool cache and database discovery

**Files:**
- Create: `MssqlMcp/Node/src/ConnectionManager.ts`
- Test: `MssqlMcp/Node/src/__tests__/ConnectionManager.test.ts`

**Step 1: Write the failing test**

```typescript
// MssqlMcp/Node/src/__tests__/ConnectionManager.test.ts
import { describe, it, expect, vi, beforeEach } from "vitest";
import { ConnectionManager } from "../ConnectionManager.js";
import type { McpConfig } from "../config.js";

// We mock the mssql module to avoid real DB connections in unit tests
vi.mock("mssql", () => {
  const mockPool = {
    connected: true,
    close: vi.fn(),
    request: () => ({
      query: vi.fn().mockResolvedValue({
        recordset: [
          { name: "DEV_airpack_HU" },
          { name: "DEV_airpack_FR" },
          { name: "PROD_airpack_HU_2019" },
          { name: "master" },
          { name: "tempdb" },
        ],
      }),
    }),
  };

  return {
    default: {
      ConnectionPool: vi.fn().mockImplementation(() => ({
        connect: vi.fn().mockResolvedValue(mockPool),
        ...mockPool,
      })),
    },
  };
});

const testConfig: McpConfig = {
  readonly: false,
  connectionTimeout: 30,
  servers: [
    { name: "SQL2019", host: "localhost", port: 50075, user: "sa", password: "pwd" },
    { name: "SQL2017", host: "localhost", port: 49990, user: "sa", password: "pwd" },
  ],
};

describe("ConnectionManager", () => {
  let manager: ConnectionManager;

  beforeEach(() => {
    manager = new ConnectionManager(testConfig);
  });

  it("discovers databases across all servers", async () => {
    const databases = await manager.listDatabases();
    // System DBs (master, tempdb, etc.) should be filtered out
    expect(databases.some(db => db.name === "DEV_airpack_HU")).toBe(true);
    expect(databases.some(db => db.name === "master")).toBe(false);
  });

  it("parses database name into components", async () => {
    const databases = await manager.listDatabases();
    const dev = databases.find(db => db.name === "DEV_airpack_HU");
    expect(dev?.environment).toBe("DEV");
    expect(dev?.application).toBe("airpack");
    expect(dev?.client).toBe("HU");
    expect(dev?.suffix).toBeNull();
  });

  it("parses database name with suffix", async () => {
    const databases = await manager.listDatabases();
    const prod = databases.find(db => db.name === "PROD_airpack_HU_2019");
    expect(prod?.environment).toBe("PROD");
    expect(prod?.application).toBe("airpack");
    expect(prod?.client).toBe("HU");
    expect(prod?.suffix).toBe("2019");
  });

  it("resolves database name to server", async () => {
    const pool = await manager.getConnection("DEV_airpack_HU");
    expect(pool).toBeDefined();
    expect(pool.connected).toBe(true);
  });

  it("throws on unknown database", async () => {
    await expect(manager.getConnection("NONEXISTENT_DB"))
      .rejects.toThrow();
  });

  it("remembers last used database", async () => {
    await manager.getConnection("DEV_airpack_HU");
    const pool = await manager.getCurrentConnection();
    expect(pool).toBeDefined();
  });

  it("returns undefined for getCurrentConnection when none used yet", async () => {
    const pool = await manager.getCurrentConnection();
    expect(pool).toBeNull();
  });
});
```

**Step 2: Run test to verify it fails**

Run: `cd MssqlMcp/Node && npx vitest run src/__tests__/ConnectionManager.test.ts`
Expected: FAIL — `../ConnectionManager.js` does not exist.

**Step 3: Write the implementation**

```typescript
// MssqlMcp/Node/src/ConnectionManager.ts
import sql from "mssql";
import type { McpConfig, ServerConfig } from "./config.js";

export interface DatabaseInfo {
  name: string;
  server: string;
  environment: string | null;
  application: string | null;
  client: string | null;
  suffix: string | null;
}

const SYSTEM_DATABASES = new Set([
  "master", "tempdb", "model", "msdb",
  "ReportServer", "ReportServerTempDB",
  "SSISDB", "distribution",
]);

const CACHE_TTL_MS = 60 * 60 * 1000; // 1 hour

export class ConnectionManager {
  private config: McpConfig;
  private pools = new Map<string, sql.ConnectionPool>();
  private databaseCache: DatabaseInfo[] | null = null;
  private cacheTimestamp = 0;
  private currentDatabase: string | null = null;

  constructor(config: McpConfig) {
    this.config = config;
  }

  async listDatabases(): Promise<DatabaseInfo[]> {
    if (this.databaseCache && Date.now() - this.cacheTimestamp < CACHE_TTL_MS) {
      return this.databaseCache;
    }

    const allDatabases: DatabaseInfo[] = [];

    for (const server of this.config.servers) {
      const pool = await this.getServerPool(server, "master");
      const result = await pool.request().query(
        "SELECT name FROM sys.databases WHERE state_desc = 'ONLINE'"
      );

      for (const row of result.recordset) {
        if (SYSTEM_DATABASES.has(row.name)) continue;
        allDatabases.push({
          ...parseDatabaseName(row.name),
          server: server.name,
        });
      }
    }

    this.databaseCache = allDatabases;
    this.cacheTimestamp = Date.now();
    return allDatabases;
  }

  async getConnection(database: string): Promise<sql.ConnectionPool> {
    const databases = await this.listDatabases();
    const entry = databases.find(db => db.name === database);

    if (!entry) {
      throw new Error(
        `Database '${database}' not found on any server. ` +
        `Available databases: ${databases.map(d => d.name).join(", ")}`
      );
    }

    const server = this.config.servers.find(s => s.name === entry.server)!;
    this.currentDatabase = database;
    return this.getServerPool(server, database);
  }

  async getCurrentConnection(): Promise<sql.ConnectionPool | null> {
    if (!this.currentDatabase) return null;
    return this.getConnection(this.currentDatabase);
  }

  getCurrentDatabase(): string | null {
    return this.currentDatabase;
  }

  async resolveConnection(database?: string): Promise<sql.ConnectionPool> {
    if (database) {
      return this.getConnection(database);
    }
    const current = await this.getCurrentConnection();
    if (!current) {
      throw new Error(
        "No database specified and no previous database selected. " +
        "Use list_databases to see available databases, then pass a 'database' parameter."
      );
    }
    return current;
  }

  private async getServerPool(
    server: ServerConfig,
    database: string
  ): Promise<sql.ConnectionPool> {
    const key = `${server.name}::${database}`;
    const existing = this.pools.get(key);
    if (existing?.connected) return existing;

    if (existing) {
      try { await existing.close(); } catch { /* ignore */ }
    }

    const config: sql.config = {
      server: server.host,
      database,
      user: server.user,
      password: server.password,
      options: {
        encrypt: true,
        trustServerCertificate: true,
        instanceName: server.instance,
      },
      connectionTimeout: this.config.connectionTimeout * 1000,
    };

    if (server.port) {
      config.port = server.port;
    }

    const pool = new sql.ConnectionPool(config);
    await pool.connect();
    this.pools.set(key, pool);
    return pool;
  }

  async closeAll(): Promise<void> {
    for (const pool of this.pools.values()) {
      try { await pool.close(); } catch { /* ignore */ }
    }
    this.pools.clear();
  }
}

export function parseDatabaseName(name: string): Omit<DatabaseInfo, "server"> {
  const parts = name.split("_");
  if (parts.length >= 3) {
    return {
      name,
      environment: parts[0],
      application: parts[1],
      client: parts[2],
      suffix: parts.length > 3 ? parts.slice(3).join("_") : null,
    };
  }
  return {
    name,
    environment: null,
    application: null,
    client: null,
    suffix: null,
  };
}
```

**Step 4: Run the test to verify it passes**

Run: `cd MssqlMcp/Node && npx vitest run src/__tests__/ConnectionManager.test.ts`
Expected: all 7 tests PASS.

**Step 5: Commit**

```bash
git add MssqlMcp/Node/src/ConnectionManager.ts MssqlMcp/Node/src/__tests__/ConnectionManager.test.ts
git commit -m "feat: add ConnectionManager with pool cache and database discovery"
```

---

### Task 3: ListDatabasesTool

**Files:**
- Create: `MssqlMcp/Node/src/tools/ListDatabasesTool.ts`
- Test: `MssqlMcp/Node/src/__tests__/ListDatabasesTool.test.ts`

**Step 1: Write the failing test**

```typescript
// MssqlMcp/Node/src/__tests__/ListDatabasesTool.test.ts
import { describe, it, expect, vi } from "vitest";
import { ListDatabasesTool } from "../tools/ListDatabasesTool.js";
import type { ConnectionManager, DatabaseInfo } from "../ConnectionManager.js";

function makeMockManager(databases: DatabaseInfo[]): ConnectionManager {
  return {
    listDatabases: vi.fn().mockResolvedValue(databases),
  } as unknown as ConnectionManager;
}

describe("ListDatabasesTool", () => {
  it("returns all databases with parsed fields", async () => {
    const manager = makeMockManager([
      { name: "DEV_airpack_HU", server: "SQL2019", environment: "DEV", application: "airpack", client: "HU", suffix: null },
      { name: "PROD_airpack_FR_2019", server: "SQL2017", environment: "PROD", application: "airpack", client: "FR", suffix: "2019" },
    ]);

    const tool = new ListDatabasesTool(manager);
    const result = await tool.execute({});
    expect(result.success).toBe(true);
    expect(result.databases).toHaveLength(2);
    expect(result.databases[0].environment).toBe("DEV");
    expect(result.databases[1].suffix).toBe("2019");
  });

  it("filters by environment", async () => {
    const manager = makeMockManager([
      { name: "DEV_airpack_HU", server: "SQL2019", environment: "DEV", application: "airpack", client: "HU", suffix: null },
      { name: "PROD_airpack_FR", server: "SQL2017", environment: "PROD", application: "airpack", client: "FR", suffix: null },
    ]);

    const tool = new ListDatabasesTool(manager);
    const result = await tool.execute({ environment: "DEV" });
    expect(result.databases).toHaveLength(1);
    expect(result.databases[0].name).toBe("DEV_airpack_HU");
  });

  it("filters by client", async () => {
    const manager = makeMockManager([
      { name: "DEV_airpack_HU", server: "SQL2019", environment: "DEV", application: "airpack", client: "HU", suffix: null },
      { name: "DEV_airpack_FR", server: "SQL2017", environment: "DEV", application: "airpack", client: "FR", suffix: null },
    ]);

    const tool = new ListDatabasesTool(manager);
    const result = await tool.execute({ client: "HU" });
    expect(result.databases).toHaveLength(1);
    expect(result.databases[0].client).toBe("HU");
  });
});
```

**Step 2: Run test to verify it fails**

Run: `cd MssqlMcp/Node && npx vitest run src/__tests__/ListDatabasesTool.test.ts`
Expected: FAIL — module does not exist.

**Step 3: Write the implementation**

```typescript
// MssqlMcp/Node/src/tools/ListDatabasesTool.ts
import { Tool } from "@modelcontextprotocol/sdk/types.js";
import type { ConnectionManager } from "../ConnectionManager.js";

export class ListDatabasesTool implements Tool {
  [key: string]: any;
  name = "list_databases";
  description =
    "Lists all available databases across all configured MSSQL servers. " +
    "Returns enriched metadata parsed from the database name (format: ENV_APPLICATION_CLIENT[_suffix]). " +
    "Use optional filters to narrow results by environment, application, or client.";

  inputSchema = {
    type: "object",
    properties: {
      environment: {
        type: "string",
        description: "Filter by environment (e.g. 'DEV', 'PROD')",
      },
      application: {
        type: "string",
        description: "Filter by application name (e.g. 'airpack')",
      },
      client: {
        type: "string",
        description: "Filter by client code (e.g. 'HU', 'FR')",
      },
    },
    required: [],
  } as any;

  private manager: ConnectionManager;

  constructor(manager: ConnectionManager) {
    this.manager = manager;
  }

  async execute(params: {
    environment?: string;
    application?: string;
    client?: string;
  }) {
    try {
      let databases = await this.manager.listDatabases();

      if (params.environment) {
        databases = databases.filter(
          db => db.environment?.toUpperCase() === params.environment!.toUpperCase()
        );
      }
      if (params.application) {
        databases = databases.filter(
          db => db.application?.toUpperCase() === params.application!.toUpperCase()
        );
      }
      if (params.client) {
        databases = databases.filter(
          db => db.client?.toUpperCase() === params.client!.toUpperCase()
        );
      }

      return {
        success: true,
        message: `Found ${databases.length} database(s)`,
        databases,
      };
    } catch (error) {
      return {
        success: false,
        message: `Failed to list databases: ${error}`,
      };
    }
  }

  async run(params: any) {
    return this.execute(params ?? {});
  }
}
```

**Step 4: Run test to verify it passes**

Run: `cd MssqlMcp/Node && npx vitest run src/__tests__/ListDatabasesTool.test.ts`
Expected: all 3 tests PASS.

**Step 5: Commit**

```bash
git add MssqlMcp/Node/src/tools/ListDatabasesTool.ts MssqlMcp/Node/src/__tests__/ListDatabasesTool.test.ts
git commit -m "feat: add list_databases tool with enriched name parsing"
```

---

### Task 4: Add `database` parameter to all existing tools

**Files:**
- Modify: `MssqlMcp/Node/src/tools/ReadDataTool.ts`
- Modify: `MssqlMcp/Node/src/tools/ListTableTool.ts`
- Modify: `MssqlMcp/Node/src/tools/DescribeTableTool.ts`
- Modify: `MssqlMcp/Node/src/tools/InsertDataTool.ts`
- Modify: `MssqlMcp/Node/src/tools/UpdateDataTool.ts`
- Modify: `MssqlMcp/Node/src/tools/CreateTableTool.ts`
- Modify: `MssqlMcp/Node/src/tools/CreateIndexTool.ts`
- Modify: `MssqlMcp/Node/src/tools/DropTableTool.ts`

Every tool currently does `new sql.Request()` which uses the implicit global pool. Each tool must instead receive a `ConnectionManager` and use `resolveConnection(database?)` to get the correct pool.

The pattern for each tool is identical. Here is the change for each:

**Step 1: Add `database` to every tool's `inputSchema.properties`**

Add to each tool's `inputSchema.properties`:

```typescript
database: {
  type: "string",
  description: "Target database name. If omitted, uses the last selected database. Use list_databases to discover available databases.",
},
```

**Step 2: Inject ConnectionManager into each tool via constructor**

Each tool gets a `private manager: ConnectionManager` field and constructor:

```typescript
private manager: ConnectionManager;

constructor(manager: ConnectionManager) {
  this.manager = manager;
}
```

**Step 3: Replace `new sql.Request()` with pool-based request**

Every occurrence of:
```typescript
const request = new sql.Request();
```
or:
```typescript
await new sql.Request().query(query);
```

Becomes:
```typescript
const pool = await this.manager.resolveConnection(params.database);
const request = pool.request();
```

Apply this to all 8 tools. The specific changes per file:

- **ReadDataTool.ts**: constructor + `resolveConnection` in `run()` (line 224-226)
- **ListTableTool.ts**: constructor + `resolveConnection` in `run()` (line 26)
- **DescribeTableTool.ts**: constructor + `resolveConnection` in `run()` (line 20)
- **InsertDataTool.ts**: constructor + `resolveConnection` in `run()` (line 96)
- **UpdateDataTool.ts**: constructor + `resolveConnection` in `run()` (line 37)
- **CreateTableTool.ts**: constructor + `resolveConnection` in `run()` (line 36)
- **CreateIndexTool.ts**: constructor + `resolveConnection` in `run()` (line 43)
- **DropTableTool.ts**: constructor + `resolveConnection` in `run()` (line 23)

**Step 4: Run all tests**

Run: `cd MssqlMcp/Node && npx vitest run`
Expected: all tests PASS.

**Step 5: Commit**

```bash
git add MssqlMcp/Node/src/tools/
git commit -m "feat: add database parameter to all tools via ConnectionManager"
```

---

### Task 5: Rewrite `index.ts` to use new config and ConnectionManager

**Files:**
- Modify: `MssqlMcp/Node/src/index.ts`

**Step 1: Rewrite index.ts**

Replace the entire file. Key changes:
- Remove `createSqlConfig`, `globalSqlPool`, `ensureSqlConnection`, `wrapToolRun`
- Import and use `loadConfig` and `ConnectionManager`
- Pass `ConnectionManager` to all tool constructors
- Add `list_databases` tool to the tool list
- Read `readonly` from config instead of env
- Register `list_databases` in both `ListToolsRequestSchema` and `CallToolRequestSchema`

```typescript
// MssqlMcp/Node/src/index.ts
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
```

**Step 2: Build and verify**

Run:
```bash
cd MssqlMcp/Node && npm run build
```
Expected: compiles with no errors.

**Step 3: Commit**

```bash
git add MssqlMcp/Node/src/index.ts
git commit -m "feat: rewrite index.ts to use config-driven multi-server architecture"
```

---

### Task 6: Update sample configs and documentation

**Files:**
- Modify: `MssqlMcp/Node/src/samples/claude_desktop_config.json`
- Modify: `MssqlMcp/Node/src/samples/vscode_agent_config.json`
- Modify: `MssqlMcp/Node/README.md`

**Step 1: Update claude_desktop_config.json**

```json
{
  "mcpServers": {
    "mssql": {
      "command": "node",
      "args": ["path/to/MssqlMcp/Node/dist/index.js"],
      "env": {
        "MSSQL_MCP_CONFIG": "/path/to/.mssql-mcp/config.json"
      }
    }
  }
}
```

**Step 2: Update vscode_agent_config.json**

```json
{
  "mcp": {
    "servers": {
      "mssql-nodejs": {
        "type": "stdio",
        "command": "node",
        "args": ["path/to/MssqlMcp/Node/dist/index.js"],
        "env": {
          "MSSQL_MCP_CONFIG": "/path/to/.mssql-mcp/config.json"
        }
      }
    }
  }
}
```

**Step 3: Update README.md**

Add a "Configuration" section documenting:
- Config file location (`~/.mssql-mcp/config.json`, override with `MSSQL_MCP_CONFIG`)
- Config file structure (servers with name/host/port or instance/user/password)
- New `list_databases` tool
- The `database` parameter on all tools
- The enriched name parsing format
- Migration guide from env-var based config

**Step 4: Commit**

```bash
git add MssqlMcp/Node/src/samples/ MssqlMcp/Node/README.md MssqlMcp/Node/mcp-config.sample.json
git commit -m "docs: update samples and README for multi-server config"
```

---

### Task 7: Final build, smoke test, cleanup

**Files:**
- Modify: `MssqlMcp/Node/.gitignore` (add `mcp-config.json`)

**Step 1: Add config file to gitignore**

Append to `MssqlMcp/Node/.gitignore`:
```
mcp-config.json
```

**Step 2: Full build**

Run:
```bash
cd MssqlMcp/Node && npm run build
```
Expected: no errors.

**Step 3: Run all tests**

Run:
```bash
cd MssqlMcp/Node && npx vitest run
```
Expected: all tests PASS.

**Step 4: Remove old env vars from dotenv usage**

Check that `import * as dotenv from "dotenv"` and the `dotenv` dependency can be removed from `index.ts` and `package.json` if no longer needed.

**Step 5: Commit**

```bash
git add .
git commit -m "chore: cleanup, gitignore config, remove dotenv dependency"
```
