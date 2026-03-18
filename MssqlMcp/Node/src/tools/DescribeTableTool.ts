import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";
import type { ConnectionManager } from "../ConnectionManager.js";

export class DescribeTableTool implements Tool {
  [key: string]: any;
  name = "describe_table";
  description = "Describes the schema (columns and types) of a specified MSSQL Database table.";

  private manager: ConnectionManager;
  constructor(manager: ConnectionManager) {
    this.manager = manager;
  }

  inputSchema = {
    type: "object",
    properties: {
      tableName: { type: "string", description: "Name of the table to describe" },
      database: {
        type: "string",
        description: "Target database name. If omitted, uses the last selected database. Use list_databases to discover available databases.",
      },
    },
    required: ["tableName"],
  } as any;

  async run(params: { tableName: string; database?: string }) {
    try {
      const { tableName } = params;
      const pool = await this.manager.resolveConnection(params.database);
      const request = pool.request();
      const query = `SELECT COLUMN_NAME as name, DATA_TYPE as type FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName`;
      request.input("tableName", sql.NVarChar, tableName);
      const result = await request.query(query);
      return {
        success: true,
        columns: result.recordset,
      };
    } catch (error) {
      return {
        success: false,
        message: `Failed to describe table: ${error}`,
      };
    }
  }
}
