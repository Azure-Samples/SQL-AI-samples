import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";
import type { ConnectionManager } from "../ConnectionManager.js";

export class ListTableTool implements Tool {
  [key: string]: any;
  name = "list_table";
  description = "Lists tables in an MSSQL Database, or list tables in specific schemas";

  private manager: ConnectionManager;
  constructor(manager: ConnectionManager) {
    this.manager = manager;
  }

  inputSchema = {
    type: "object",
    properties: {
      parameters: { 
        type: "array", 
        description: "Schemas to filter by (optional)",
        items: {
          type: "string"
        },
        minItems: 0
      },
      database: {
        type: "string",
        description: "Target database name. If omitted, uses the last selected database. Use list_databases to discover available databases.",
      },
    },
    required: [],
  } as any;

  async run(params: any) {
    try {
      const { parameters } = params;
      const pool = await this.manager.resolveConnection(params.database);
      const request = pool.request();
      let schemaFilter = "";
      if (parameters && parameters.length > 0) {
        parameters.forEach((p: string, i: number) => {
          request.input(`schema${i}`, sql.NVarChar, p);
        });
        const placeholders = parameters.map((_: string, i: number) => `@schema${i}`).join(", ");
        schemaFilter = `AND TABLE_SCHEMA IN (${placeholders})`;
      }
      const query = `SELECT TABLE_SCHEMA + '.' + TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ${schemaFilter} ORDER BY TABLE_SCHEMA, TABLE_NAME`;
      const result = await request.query(query);
      return {
        success: true,
        message: `List tables executed successfully`,
        items: result.recordset,
      };
    } catch (error) {
      console.error("Error listing tables:", error);
      return {
        success: false,
        message: `Failed to list tables: ${error}`,
      };
    }
  }
}
