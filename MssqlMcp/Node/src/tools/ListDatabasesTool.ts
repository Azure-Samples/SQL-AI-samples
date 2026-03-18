import { Tool } from "@modelcontextprotocol/sdk/types.js";
import type { ConnectionManager, DatabaseInfo } from "../ConnectionManager.js";

export class ListDatabasesTool implements Tool {
  [key: string]: unknown;
  name = "list_databases";
  description =
    "Lists all available databases across configured servers with enriched metadata (environment, application, client parsed from names).";
  inputSchema = {
    type: "object",
    properties: {
      environment: {
        type: "string",
        description: "Filter by environment (case-insensitive)",
      },
      application: {
        type: "string",
        description: "Filter by application (case-insensitive)",
      },
      client: {
        type: "string",
        description: "Filter by client (case-insensitive)",
      },
    },
    required: [],
  } as const;

  constructor(private manager: ConnectionManager) {}

  private matchesFilter(
    db: DatabaseInfo,
    key: keyof Pick<DatabaseInfo, "environment" | "application" | "client">,
    value: string
  ): boolean {
    const field = db[key];
    if (field == null) return false;
    return field.toLowerCase() === value.toLowerCase();
  }

  async run(params: {
    environment?: string;
    application?: string;
    client?: string;
  }): Promise<
    | { success: true; message: string; databases: DatabaseInfo[] }
    | { success: false; message: string }
  > {
    try {
      const dbs = await this.manager.listDatabases();

      let filtered = dbs;
      if (params?.environment) {
        filtered = filtered.filter((db) =>
          this.matchesFilter(db, "environment", params.environment!)
        );
      }
      if (params?.application) {
        filtered = filtered.filter((db) =>
          this.matchesFilter(db, "application", params.application!)
        );
      }
      if (params?.client) {
        filtered = filtered.filter((db) =>
          this.matchesFilter(db, "client", params.client!)
        );
      }

      return {
        success: true,
        message: `Found ${filtered.length} database(s)`,
        databases: filtered,
      };
    } catch (error) {
      const msg = error instanceof Error ? error.message : String(error);
      return {
        success: false,
        message: `Failed to list databases: ${msg}`,
      };
    }
  }
}
