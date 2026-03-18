import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { loadConfig } from "../config.js";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";

function writeTempConfig(dir: string, config: unknown): string {
  const filePath = path.join(dir, "config.json");
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(config));
  return filePath;
}

const validConfig = {
  servers: [
    { name: "SQL2019", host: "localhost", port: 50075, user: "sa", password: "pass" },
  ],
};

const multiServerConfig = {
  readonly: true,
  connectionTimeout: 60,
  servers: [
    { name: "SQL2019", host: "localhost", port: 50075, user: "sa", password: "pass1" },
    { name: "SQLPROD", host: "prodserver", instance: "SQLEXPRESS", user: "sa", password: "pass2" },
  ],
};

describe("loadConfig", () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), "mssql-mcp-test-"));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
    vi.restoreAllMocks();
  });

  // --- Path resolution ---

  it("loads config from an explicit path", () => {
    const configPath = writeTempConfig(tmpDir, validConfig);
    const config = loadConfig(configPath);
    expect(config.servers).toHaveLength(1);
    expect(config.servers[0].name).toBe("SQL2019");
  });

  it("falls back to MSSQL_MCP_CONFIG env var when no path is given", () => {
    const configPath = writeTempConfig(tmpDir, validConfig);
    vi.stubEnv("MSSQL_MCP_CONFIG", configPath);
    const config = loadConfig();
    expect(config.servers[0].name).toBe("SQL2019");
  });

  it("falls back to ~/.mssql-mcp/config.json when no path and no env var", () => {
    const fakeHome = tmpDir;
    vi.stubEnv("HOME", fakeHome);
    vi.stubEnv("MSSQL_MCP_CONFIG", "");
    const defaultDir = path.join(fakeHome, ".mssql-mcp");
    writeTempConfig(defaultDir, validConfig);
    const config = loadConfig();
    expect(config.servers[0].name).toBe("SQL2019");
  });

  it("throws when config file does not exist", () => {
    expect(() => loadConfig("/nonexistent/path/config.json")).toThrow(/not found|ENOENT/i);
  });

  // --- Defaults ---

  it("defaults readonly to false", () => {
    const configPath = writeTempConfig(tmpDir, validConfig);
    const config = loadConfig(configPath);
    expect(config.readonly).toBe(false);
  });

  it("defaults connectionTimeout to 30", () => {
    const configPath = writeTempConfig(tmpDir, validConfig);
    const config = loadConfig(configPath);
    expect(config.connectionTimeout).toBe(30);
  });

  it("preserves explicit values over defaults", () => {
    const configPath = writeTempConfig(tmpDir, multiServerConfig);
    const config = loadConfig(configPath);
    expect(config.readonly).toBe(true);
    expect(config.connectionTimeout).toBe(60);
  });

  // --- Multi-server ---

  it("loads multiple servers", () => {
    const configPath = writeTempConfig(tmpDir, multiServerConfig);
    const config = loadConfig(configPath);
    expect(config.servers).toHaveLength(2);
    expect(config.servers[0].port).toBe(50075);
    expect(config.servers[1].instance).toBe("SQLEXPRESS");
  });

  // --- Validation: servers array ---

  it("throws when servers is missing", () => {
    const configPath = writeTempConfig(tmpDir, {});
    expect(() => loadConfig(configPath)).toThrow(/servers/i);
  });

  it("throws when servers is empty", () => {
    const configPath = writeTempConfig(tmpDir, { servers: [] });
    expect(() => loadConfig(configPath)).toThrow(/servers/i);
  });

  it("throws when servers is not an array", () => {
    const configPath = writeTempConfig(tmpDir, { servers: "nope" });
    expect(() => loadConfig(configPath)).toThrow(/servers/i);
  });

  // --- Validation: required server fields ---

  it.each(["name", "host", "user", "password"])("throws when server is missing '%s'", (field) => {
    const server: Record<string, unknown> = {
      name: "S1", host: "localhost", port: 1433, user: "sa", password: "pass",
    };
    delete server[field];
    const configPath = writeTempConfig(tmpDir, { servers: [server] });
    expect(() => loadConfig(configPath)).toThrow(new RegExp(field, "i"));
  });

  // --- Validation: port or instance ---

  it("throws when server has neither port nor instance", () => {
    const configPath = writeTempConfig(tmpDir, {
      servers: [{ name: "S1", host: "localhost", user: "sa", password: "pass" }],
    });
    expect(() => loadConfig(configPath)).toThrow(/port.*instance|instance.*port/i);
  });

  it("accepts a server with port but no instance", () => {
    const configPath = writeTempConfig(tmpDir, {
      servers: [{ name: "S1", host: "localhost", port: 1433, user: "sa", password: "pass" }],
    });
    const config = loadConfig(configPath);
    expect(config.servers[0].port).toBe(1433);
    expect(config.servers[0].instance).toBeUndefined();
  });

  it("accepts a server with instance but no port", () => {
    const configPath = writeTempConfig(tmpDir, {
      servers: [{ name: "S1", host: "localhost", instance: "SQLEXPRESS", user: "sa", password: "pass" }],
    });
    const config = loadConfig(configPath);
    expect(config.servers[0].instance).toBe("SQLEXPRESS");
    expect(config.servers[0].port).toBeUndefined();
  });

  it("throws when port is a string instead of number", () => {
    const configPath = writeTempConfig(tmpDir, {
      servers: [{ name: "S1", host: "localhost", port: "1433", user: "sa", password: "pass" }],
    });
    expect(() => loadConfig(configPath)).toThrow("Server 'S1': 'port' must be a number");
  });

  // --- Validation: invalid JSON ---

  it("throws on invalid JSON", () => {
    const filePath = path.join(tmpDir, "bad.json");
    fs.writeFileSync(filePath, "not json{{{");
    expect(() => loadConfig(filePath)).toThrow(/parse|json/i);
  });
});
