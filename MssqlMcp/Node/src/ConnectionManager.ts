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

/** Lowercase names for case-insensitive matching; SQL Server can return names in different cases */
export const SYSTEM_DATABASES = new Set([
  "master",
  "tempdb",
  "model",
  "msdb",
  "reportserver",
  "reportservertempdb",
  "ssisdb",
  "distribution",
]);

export const CACHE_TTL_MS = 60 * 60 * 1000;

export function parseDatabaseName(
  name: string
): Omit<DatabaseInfo, "server"> {
  const parts = name.split("_");

  if (parts.length < 3) {
    return { name, environment: null, application: null, client: null, suffix: null };
  }

  return {
    name,
    environment: parts[0],
    application: parts[1],
    client: parts[2],
    suffix: parts.length > 3 ? parts.slice(3).join("_") : null,
  };
}

export class ConnectionManager {
  private config: McpConfig;
  private pools = new Map<string, sql.ConnectionPool>();
  private currentDatabase: string | null = null;
  private dbCache: DatabaseInfo[] | null = null;
  private dbCacheTime = 0;

  constructor(config: McpConfig) {
    this.config = config;
  }

  async listDatabases(): Promise<DatabaseInfo[]> {
    if (this.dbCache && Date.now() - this.dbCacheTime < CACHE_TTL_MS) {
      return this.dbCache;
    }

    const results: DatabaseInfo[] = [];

    for (const server of this.config.servers) {
      const pool = await this.getServerPool(server, "master");
      const result = await pool.request().query(
        "SELECT name FROM sys.databases WHERE state_desc = 'ONLINE'"
      );

      for (const row of result.recordset) {
        if (SYSTEM_DATABASES.has(row.name.toLowerCase())) continue;

        results.push({
          ...parseDatabaseName(row.name),
          server: server.name,
        });
      }
    }

    this.dbCache = results;
    this.dbCacheTime = Date.now();
    return results;
  }

  async getConnection(database: string): Promise<sql.ConnectionPool> {
    const dbs = await this.listDatabases();
    const info = dbs.find((d) => d.name === database);

    if (!info) {
      const available = dbs.map((d) => d.name).join(", ");
      throw new Error(
        `Database "${database}" not found. Available databases: ${available}`
      );
    }

    const server = this.config.servers.find((s) => s.name === info.server)!;
    this.currentDatabase = database;
    return this.getServerPool(server, database);
  }

  async getCurrentConnection(): Promise<sql.ConnectionPool | null> {
    if (!this.currentDatabase) return null;

    const dbs = await this.listDatabases();
    const info = dbs.find((d) => d.name === this.currentDatabase);
    if (!info) return null;

    const server = this.config.servers.find((s) => s.name === info.server)!;
    return this.getServerPool(server, this.currentDatabase);
  }

  getCurrentDatabase(): string | null {
    return this.currentDatabase;
  }

  async resolveConnection(database?: string): Promise<sql.ConnectionPool> {
    if (database) {
      return this.getConnection(database);
    }

    const pool = await this.getCurrentConnection();
    if (pool) return pool;

    throw new Error(
      "No database selected. Use list_databases to see available databases, then specify one."
    );
  }

  async closeAll(): Promise<void> {
    const closeTasks: Promise<void>[] = [];

    for (const pool of this.pools.values()) {
      if (pool.connected) {
        closeTasks.push(pool.close());
      }
    }

    await Promise.all(closeTasks);
    this.pools.clear();
    this.currentDatabase = null;
  }

  private async getServerPool(
    server: ServerConfig,
    database: string
  ): Promise<sql.ConnectionPool> {
    const key = `${server.name}::${database}`;
    const existing = this.pools.get(key);

    if (existing?.connected) {
      return existing;
    }

    if (existing) {
      try {
        await existing.close();
      } catch {
        // ignore close errors on stale pools
      }
    }

    const serverAddress = server.instance
      ? `${server.host}\\${server.instance}`
      : server.host;

    const poolConfig: sql.config = {
      server: serverAddress,
      database,
      user: server.user,
      password: server.password,
      options: {
        encrypt: true,
        trustServerCertificate: true,
      },
      connectionTimeout: this.config.connectionTimeout * 1000,
    };

    if (server.port) {
      poolConfig.port = server.port;
    }

    const pool = new sql.ConnectionPool(poolConfig);
    await pool.connect();
    this.pools.set(key, pool);
    return pool;
  }
}
