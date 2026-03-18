import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";
import type { ConnectionManager } from "../ConnectionManager.js";

export class DropTableTool implements Tool {
  [key: string]: any;
  name = "drop_table";
  description = "Drops a table from the MSSQL Database.";

  private manager: ConnectionManager;
  constructor(manager: ConnectionManager) {
    this.manager = manager;
  }

  inputSchema = {
    type: "object",
    properties: {
      tableName: { type: "string", description: "Name of the table to drop" },
      database: {
        type: "string",
        description: "Target database name. If omitted, uses the last selected database. Use list_databases to discover available databases.",
      },
    },
    required: ["tableName"],
  } as any;

  async run(params: any) {
    try {
      const { tableName } = params;
      if (!/^[\w\d_]+$/.test(tableName)) {
        throw new Error("Invalid table name.");
      }
      const query = `DROP TABLE [${tableName}]`;
      const pool = await this.manager.resolveConnection(params.database);
      await pool.request().query(query);
      return {
        success: true,
        message: `Table '${tableName}' dropped successfully.`
      };
    } catch (error) {
      console.error("Error dropping table:", error);
      return {
        success: false,
        message: `Failed to drop table: ${error}`
      };
    }
  }
}