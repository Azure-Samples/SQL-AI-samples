import fs from "node:fs";
import path from "node:path";
import os from "node:os";

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

function resolveConfigPath(configPath?: string): string {
  if (configPath) return configPath;

  const envPath = process.env.MSSQL_MCP_CONFIG;
  if (envPath) return envPath;

  return path.join(os.homedir(), ".mssql-mcp", "config.json");
}

function validateServer(server: unknown, index: number): ServerConfig {
  const s = server as Record<string, unknown>;
  const required = ["name", "host", "user", "password"] as const;

  for (const field of required) {
    if (!s[field] || typeof s[field] !== "string") {
      throw new Error(
        `Server at index ${index}: missing or invalid required field "${field}"`
      );
    }
  }

  if (s.port != null && typeof s.port !== "number") {
    throw new Error(`Server '${s.name}': 'port' must be a number`);
  }

  if (s.port == null && (!s.instance || typeof s.instance !== "string")) {
    throw new Error(
      `Server "${s.name}" (index ${index}): must have either port or instance`
    );
  }

  return {
    name: s.name as string,
    host: s.host as string,
    port: typeof s.port === "number" ? s.port : undefined,
    instance: typeof s.instance === "string" ? s.instance : undefined,
    user: s.user as string,
    password: s.password as string,
  };
}

export function loadConfig(configPath?: string): McpConfig {
  const resolved = resolveConfigPath(configPath);

  let raw: string;
  try {
    raw = fs.readFileSync(resolved, "utf-8");
  } catch (err: unknown) {
    const code = (err as NodeJS.ErrnoException).code;
    if (code === "ENOENT") {
      throw new Error(`Config file not found: ${resolved}`);
    }
    throw err;
  }

  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    throw new Error(`Failed to parse config file as JSON: ${resolved}`);
  }

  const obj = parsed as Record<string, unknown>;

  if (!Array.isArray(obj.servers) || obj.servers.length === 0) {
    throw new Error(
      "Config must contain a non-empty \"servers\" array"
    );
  }

  const servers = obj.servers.map((s: unknown, i: number) => validateServer(s, i));

  return {
    readonly: typeof obj.readonly === "boolean" ? obj.readonly : false,
    connectionTimeout:
      typeof obj.connectionTimeout === "number" ? obj.connectionTimeout : 30,
    servers,
  };
}
