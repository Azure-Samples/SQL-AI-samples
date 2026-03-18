import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import type { McpConfig, ServerConfig } from "../config.js";

function makeServer(overrides: Partial<ServerConfig> = {}): ServerConfig {
  return {
    name: "SQL2019",
    host: "localhost",
    port: 50075,
    user: "sa",
    password: "pass",
    ...overrides,
  };
}

function makeConfig(overrides: Partial<McpConfig> = {}): McpConfig {
  return {
    readonly: false,
    connectionTimeout: 30,
    servers: [makeServer()],
    ...overrides,
  };
}

const mockClose = vi.fn().mockResolvedValue(undefined);
const mockConnect = vi.fn();
const mockQuery = vi.fn();

vi.mock("mssql", () => {
  class MockConnectionPool {
    config: unknown;
    connected = false;

    constructor(config: unknown) {
      this.config = config;
    }

    async connect() {
      mockConnect(this.config);
      this.connected = true;
      return this;
    }

    async close() {
      this.connected = false;
      return mockClose();
    }

    request() {
      return { query: mockQuery };
    }
  }

  return { default: { ConnectionPool: MockConnectionPool } };
});

let ConnectionManager: typeof import("../ConnectionManager.js").ConnectionManager;
let parseDatabaseName: typeof import("../ConnectionManager.js").parseDatabaseName;
let SYSTEM_DATABASES: typeof import("../ConnectionManager.js").SYSTEM_DATABASES;
let CACHE_TTL_MS: typeof import("../ConnectionManager.js").CACHE_TTL_MS;

beforeEach(async () => {
  vi.resetModules();

  const mod = await import("../ConnectionManager.js");
  ConnectionManager = mod.ConnectionManager;
  parseDatabaseName = mod.parseDatabaseName;
  SYSTEM_DATABASES = mod.SYSTEM_DATABASES;
  CACHE_TTL_MS = mod.CACHE_TTL_MS;
});

afterEach(() => {
  vi.restoreAllMocks();
  mockClose.mockReset();
  mockConnect.mockReset();
  mockQuery.mockReset();
});

// --- parseDatabaseName ---

describe("parseDatabaseName", () => {
  it("parses name with >= 3 underscore-separated parts", () => {
    const result = parseDatabaseName("DEV_App_Client1");
    expect(result).toEqual({
      name: "DEV_App_Client1",
      environment: "DEV",
      application: "App",
      client: "Client1",
      suffix: null,
    });
  });

  it("includes suffix when 4+ parts", () => {
    const result = parseDatabaseName("PROD_App_Client1_archive_2024");
    expect(result).toEqual({
      name: "PROD_App_Client1_archive_2024",
      environment: "PROD",
      application: "App",
      client: "Client1",
      suffix: "archive_2024",
    });
  });

  it("returns all nulls for names with < 3 parts", () => {
    const result = parseDatabaseName("SomeDB");
    expect(result).toEqual({
      name: "SomeDB",
      environment: null,
      application: null,
      client: null,
      suffix: null,
    });
  });

  it("returns all nulls for two-part names", () => {
    const result = parseDatabaseName("DEV_App");
    expect(result).toEqual({
      name: "DEV_App",
      environment: null,
      application: null,
      client: null,
      suffix: null,
    });
  });
});

// --- Constants ---

describe("constants", () => {
  it("SYSTEM_DATABASES contains expected system databases (lowercase for case-insensitive matching)", () => {
    expect(SYSTEM_DATABASES.has("master")).toBe(true);
    expect(SYSTEM_DATABASES.has("tempdb")).toBe(true);
    expect(SYSTEM_DATABASES.has("model")).toBe(true);
    expect(SYSTEM_DATABASES.has("msdb")).toBe(true);
    expect(SYSTEM_DATABASES.has("ssisdb")).toBe(true);
  });

  it("CACHE_TTL_MS is 1 hour", () => {
    expect(CACHE_TTL_MS).toBe(60 * 60 * 1000);
  });
});

// --- ConnectionManager ---

describe("ConnectionManager", () => {
  describe("listDatabases", () => {
    it("discovers databases from configured servers, filtering system DBs", async () => {
      mockQuery.mockResolvedValue({
        recordset: [
          { name: "master" },
          { name: "tempdb" },
          { name: "DEV_App_Client1" },
          { name: "PROD_App_Client2" },
        ],
      });

      const cm = new ConnectionManager(makeConfig());
      const dbs = await cm.listDatabases();

      expect(dbs).toHaveLength(2);
      expect(dbs[0]).toEqual({
        name: "DEV_App_Client1",
        server: "SQL2019",
        environment: "DEV",
        application: "App",
        client: "Client1",
        suffix: null,
      });
      expect(dbs[1]).toEqual({
        name: "PROD_App_Client2",
        server: "SQL2019",
        environment: "PROD",
        application: "App",
        client: "Client2",
        suffix: null,
      });
    });

    it("filters out system databases regardless of name casing (e.g. Master vs master)", async () => {
      // SQL Server can return names in different cases; filtering must be case-insensitive
      mockQuery.mockResolvedValue({
        recordset: [
          { name: "Master" }, // capital M - should be filtered out
          { name: "tempdb" },
          { name: "DEV_App_Client1" },
        ],
      });

      const cm = new ConnectionManager(makeConfig());
      const dbs = await cm.listDatabases();

      const names = dbs.map((d) => d.name);
      expect(names).not.toContain("Master");
      expect(names).not.toContain("master");
      expect(names).toContain("DEV_App_Client1");
      expect(dbs).toHaveLength(1);
    });

    it("discovers databases across multiple servers", async () => {
      const config = makeConfig({
        servers: [
          makeServer({ name: "SERVER_A" }),
          makeServer({ name: "SERVER_B", host: "other-host", port: 1434 }),
        ],
      });

      let callCount = 0;
      mockQuery.mockImplementation(() => {
        callCount++;
        if (callCount === 1) {
          return { recordset: [{ name: "DB_A" }] };
        }
        return { recordset: [{ name: "DB_B" }] };
      });

      const cm = new ConnectionManager(config);
      const dbs = await cm.listDatabases();

      expect(dbs).toHaveLength(2);
      expect(dbs.map((d) => d.name)).toEqual(["DB_A", "DB_B"]);
      expect(dbs[0].server).toBe("SERVER_A");
      expect(dbs[1].server).toBe("SERVER_B");
    });

    it("caches results and does not re-query within TTL", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "UserDB" }],
      });

      const cm = new ConnectionManager(makeConfig());
      await cm.listDatabases();
      await cm.listDatabases();

      expect(mockQuery).toHaveBeenCalledTimes(1);
    });

    it("re-queries after cache expires", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "UserDB" }],
      });

      const cm = new ConnectionManager(makeConfig());
      await cm.listDatabases();

      vi.useFakeTimers();
      vi.advanceTimersByTime(CACHE_TTL_MS + 1);

      await cm.listDatabases();
      vi.useRealTimers();

      expect(mockQuery).toHaveBeenCalledTimes(2);
    });
  });

  describe("getConnection", () => {
    it("returns a pool for a known database", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "MyDB" }],
      });

      const cm = new ConnectionManager(makeConfig());
      const pool = await cm.getConnection("MyDB");

      expect(pool).toBeDefined();
      expect(pool.connected).toBe(true);
    });

    it("sets currentDatabase after connecting", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "MyDB" }],
      });

      const cm = new ConnectionManager(makeConfig());
      await cm.getConnection("MyDB");

      expect(cm.getCurrentDatabase()).toBe("MyDB");
    });

    it("throws when database is not found, listing available databases", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "ExistingDB" }],
      });

      const cm = new ConnectionManager(makeConfig());

      await expect(cm.getConnection("UnknownDB")).rejects.toThrow(/UnknownDB/);
      await expect(cm.getConnection("UnknownDB")).rejects.toThrow(/ExistingDB/);
    });
  });

  describe("getCurrentConnection / getCurrentDatabase", () => {
    it("returns null when no database has been used", async () => {
      const cm = new ConnectionManager(makeConfig());
      expect(cm.getCurrentDatabase()).toBeNull();
      expect(await cm.getCurrentConnection()).toBeNull();
    });

    it("returns the pool for the last-used database", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "MyDB" }],
      });

      const cm = new ConnectionManager(makeConfig());
      await cm.getConnection("MyDB");

      const pool = await cm.getCurrentConnection();
      expect(pool).toBeDefined();
      expect(pool!.connected).toBe(true);
    });
  });

  describe("resolveConnection", () => {
    it("uses explicit database when provided", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "MyDB" }],
      });

      const cm = new ConnectionManager(makeConfig());
      const pool = await cm.resolveConnection("MyDB");

      expect(pool.connected).toBe(true);
      expect(cm.getCurrentDatabase()).toBe("MyDB");
    });

    it("uses current database when none provided", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "MyDB" }],
      });

      const cm = new ConnectionManager(makeConfig());
      await cm.getConnection("MyDB");
      const pool = await cm.resolveConnection();

      expect(pool.connected).toBe(true);
    });

    it("throws descriptive error when no database provided and none current", async () => {
      const cm = new ConnectionManager(makeConfig());

      await expect(cm.resolveConnection()).rejects.toThrow(/list_databases/i);
    });
  });

  describe("getServerPool (via getConnection)", () => {
    it("reuses existing connected pool for same database", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "MyDB" }],
      });

      const cm = new ConnectionManager(makeConfig());
      const pool1 = await cm.getConnection("MyDB");
      const pool2 = await cm.getConnection("MyDB");

      expect(pool1).toBe(pool2);
    });

    it("creates pool with correct config for port-based server", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "MyDB" }],
      });

      const server = makeServer({ host: "myhost", port: 9999 });
      const cm = new ConnectionManager(makeConfig({ servers: [server] }));
      await cm.getConnection("MyDB");

      const poolConfig = mockConnect.mock.calls.find(
        (call: unknown[]) => (call[0] as Record<string, unknown>).database === "MyDB"
      );
      expect(poolConfig).toBeDefined();
      const cfg = poolConfig![0] as Record<string, unknown>;
      expect(cfg.server).toBe("myhost");
      expect(cfg.port).toBe(9999);
      expect(cfg.database).toBe("MyDB");
    });

    it("creates pool with instance for instance-based server", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "MyDB" }],
      });

      const server = makeServer({
        host: "myhost",
        port: undefined,
        instance: "SQLEXPRESS",
      });
      const cm = new ConnectionManager(makeConfig({ servers: [server] }));
      await cm.getConnection("MyDB");

      const poolConfig = mockConnect.mock.calls.find(
        (call: unknown[]) => (call[0] as Record<string, unknown>).database === "MyDB"
      );
      expect(poolConfig).toBeDefined();
      const cfg = poolConfig![0] as Record<string, unknown>;
      expect(cfg.server).toBe("myhost\\SQLEXPRESS");
    });
  });

  describe("closeAll", () => {
    it("closes all open pools", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "DB1" }, { name: "DB2" }],
      });

      const cm = new ConnectionManager(makeConfig());
      await cm.getConnection("DB1");
      await cm.getConnection("DB2");

      await cm.closeAll();

      // master pool + DB1 pool + DB2 pool = 3
      expect(mockClose).toHaveBeenCalled();
    });

    it("resets currentDatabase after closeAll", async () => {
      mockQuery.mockResolvedValue({
        recordset: [{ name: "DB1" }],
      });

      const cm = new ConnectionManager(makeConfig());
      await cm.getConnection("DB1");
      await cm.closeAll();

      expect(cm.getCurrentDatabase()).toBeNull();
    });
  });
});
