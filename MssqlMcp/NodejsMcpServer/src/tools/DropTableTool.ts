import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

export class DropTableTool implements Tool {
  [key: string]: any;
  name = "drop_table";
  description = "Drops a table from the MSSQL Database.";
  inputSchema = {
    type: "object",
    properties: {
      tableName: { type: "string", description: "Name of the table to drop" }
    },
    required: ["tableName"],
  } as any;

  async run(params: any) {
    try {
      const { tableName } = params;
      // Basic validation to prevent SQL injection
      if (!/^[\w\d_]+$/.test(tableName)) {
        throw new Error("Invalid table name.");
      }
      const query = `DROP TABLE [${tableName}]`;
      await new sql.Request().query(query);
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