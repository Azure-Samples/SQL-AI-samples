import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";


export class DescribeTableTool implements Tool {
  [key: string]: any;
  name = "describe_table";
  description = "Describes the schema (columns and types) of a specified MSSQL Database table.";
  inputSchema = {
    type: "object",
    properties: {
      tableName: { type: "string", description: "Name of the table to describe" },
    },
    required: ["tableName"],
  } as any;

  async run(params: { tableName: string }) {
    try {
      const { tableName } = params;
      // Split "dbo.Users" into ["dbo", "Users"]
      const parts = tableName.split('.');
      const name = parts.pop();
      const schema = parts.pop() || 'dbo'; // Default to 'dbo' if no schema provided
      const request = new sql.Request();
      const query = `SELECT COLUMN_NAME as name, DATA_TYPE as type FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND TABLE_SCHEMA = @tableSchema`;
      request.input("tableName", sql.NVarChar, name);
      request.input("tableSchema", sql.NVarChar, schema);
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
