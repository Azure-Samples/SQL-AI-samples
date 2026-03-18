import { describe, it, expect, vi } from "vitest";
import { ListDatabasesTool } from "../tools/ListDatabasesTool.js";
import type { ConnectionManager } from "../ConnectionManager.js";
import type { DatabaseInfo } from "../ConnectionManager.js";

function makeDb(overrides: Partial<DatabaseInfo> = {}): DatabaseInfo {
  return {
    name: "DEV_App_Client1",
    server: "SQL2019",
    environment: "DEV",
    application: "App",
    client: "Client1",
    suffix: null,
    ...overrides,
  };
}

describe("ListDatabasesTool", () => {
  it("returns all databases with enriched metadata when no filters provided", async () => {
    const dbs: DatabaseInfo[] = [
      makeDb(),
      makeDb({
        name: "PROD_App_Client2",
        server: "SQL2019",
        environment: "PROD",
        application: "App",
        client: "Client2",
      }),
    ];

    const manager = {
      listDatabases: vi.fn().mockResolvedValue(dbs),
    } as unknown as ConnectionManager;

    const tool = new ListDatabasesTool(manager);
    const result = await tool.run({});

    expect(result).toEqual({
      success: true,
      message: "Found 2 database(s)",
      databases: dbs,
    });
    expect(manager.listDatabases).toHaveBeenCalledTimes(1);
  });

  it("applies case-insensitive filter for environment when provided", async () => {
    const dbs: DatabaseInfo[] = [
      makeDb({ environment: "DEV" }),
      makeDb({
        name: "PROD_App_Client2",
        environment: "PROD",
        application: "App",
        client: "Client2",
      }),
    ];

    const manager = {
      listDatabases: vi.fn().mockResolvedValue(dbs),
    } as unknown as ConnectionManager;

    const tool = new ListDatabasesTool(manager);
    const result = await tool.run({ environment: "dev" });

    expect(result.success).toBe(true);
    expect((result as { databases: DatabaseInfo[] }).databases).toHaveLength(1);
    expect((result as { databases: DatabaseInfo[] }).databases[0].environment).toBe("DEV");
    expect((result as { message: string }).message).toBe("Found 1 database(s)");
  });

  it("applies case-insensitive filter for application when provided", async () => {
    const dbs: DatabaseInfo[] = [
      makeDb({ application: "App" }),
      makeDb({
        name: "DEV_Other_Client2",
        environment: "DEV",
        application: "Other",
        client: "Client2",
      }),
    ];

    const manager = {
      listDatabases: vi.fn().mockResolvedValue(dbs),
    } as unknown as ConnectionManager;

    const tool = new ListDatabasesTool(manager);
    const result = await tool.run({ application: "APP" });

    expect(result.success).toBe(true);
    expect((result as { databases: DatabaseInfo[] }).databases).toHaveLength(1);
    expect((result as { databases: DatabaseInfo[] }).databases[0].application).toBe("App");
  });

  it("applies case-insensitive filter for client when provided", async () => {
    const dbs: DatabaseInfo[] = [
      makeDb({ client: "Client1" }),
      makeDb({
        name: "DEV_App_Client2",
        environment: "DEV",
        application: "App",
        client: "Client2",
      }),
    ];

    const manager = {
      listDatabases: vi.fn().mockResolvedValue(dbs),
    } as unknown as ConnectionManager;

    const tool = new ListDatabasesTool(manager);
    const result = await tool.run({ client: "client2" });

    expect(result.success).toBe(true);
    expect((result as { databases: DatabaseInfo[] }).databases).toHaveLength(1);
    expect((result as { databases: DatabaseInfo[] }).databases[0].client).toBe("Client2");
  });

  it("combines multiple filters when all provided", async () => {
    const dbs: DatabaseInfo[] = [
      makeDb({ environment: "DEV", application: "App", client: "Client1" }),
      makeDb({
        name: "PROD_App_Client1",
        environment: "PROD",
        application: "App",
        client: "Client1",
      }),
    ];

    const manager = {
      listDatabases: vi.fn().mockResolvedValue(dbs),
    } as unknown as ConnectionManager;

    const tool = new ListDatabasesTool(manager);
    const result = await tool.run({
      environment: "DEV",
      application: "App",
      client: "Client1",
    });

    expect(result.success).toBe(true);
    expect((result as { databases: DatabaseInfo[] }).databases).toHaveLength(1);
    expect((result as { databases: DatabaseInfo[] }).databases[0].name).toBe("DEV_App_Client1");
  });

  it("returns empty list when filters match no databases", async () => {
    const dbs: DatabaseInfo[] = [makeDb({ environment: "DEV" })];

    const manager = {
      listDatabases: vi.fn().mockResolvedValue(dbs),
    } as unknown as ConnectionManager;

    const tool = new ListDatabasesTool(manager);
    const result = await tool.run({ environment: "PROD" });

    expect(result).toEqual({
      success: true,
      message: "Found 0 database(s)",
      databases: [],
    });
  });

  it("handles null environment/application/client in DatabaseInfo (unparsed names)", async () => {
    const dbs: DatabaseInfo[] = [
      makeDb({
        name: "LegacyDB",
        environment: null,
        application: null,
        client: null,
      }),
    ];

    const manager = {
      listDatabases: vi.fn().mockResolvedValue(dbs),
    } as unknown as ConnectionManager;

    const tool = new ListDatabasesTool(manager);
    const resultWithFilter = await tool.run({ environment: "DEV" });
    expect((resultWithFilter as { databases: DatabaseInfo[] }).databases).toHaveLength(0);

    const resultNoFilter = await tool.run({});
    expect((resultNoFilter as { databases: DatabaseInfo[] }).databases).toHaveLength(1);
  });

  it("returns failure when listDatabases throws", async () => {
    const manager = {
      listDatabases: vi.fn().mockRejectedValue(new Error("Connection refused")),
    } as unknown as ConnectionManager;

    const tool = new ListDatabasesTool(manager);
    const result = await tool.run({});

    expect(result).toEqual({
      success: false,
      message: "Failed to list databases: Connection refused",
    });
  });

  it("has correct MCP tool metadata", () => {
    const manager = { listDatabases: vi.fn() } as unknown as ConnectionManager;
    const tool = new ListDatabasesTool(manager);

    expect(tool.name).toBe("list_databases");
    expect(tool.description.toLowerCase()).toContain("list");
    expect(tool.description.toLowerCase()).toContain("database");
    expect(tool.inputSchema.properties).toHaveProperty("environment");
    expect(tool.inputSchema.properties).toHaveProperty("application");
    expect(tool.inputSchema.properties).toHaveProperty("client");
  });
});
