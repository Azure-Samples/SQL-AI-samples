import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

export class QueryTableTool implements Tool {
  [key: string]: any;
  name = "query_table";
  description = "Queries an Azure SQL Database table using SQL syntax";
  inputSchema = {
    type: "object",
    properties: {
      query: { type: "string", description: "SQL query string" },
      parameters: { type: "array", description: "Query parameters" },
    },
    required: ["query"],
  } as any;

  async run(params: any) {
    try {
      const { query, parameters } = params;
      const request = new sql.Request();
      (parameters || []).forEach((param: any, index: number) => {
        request.input(`param${index}`, param);
      });
      const result = await request.query(query);
      return {
        success: true,
        message: `Query executed successfully`,
        items: result.recordset,
      };
    } catch (error) {
      console.error("Error querying table:", error);
      return {
        success: false,
        message: `Failed to query table: ${error}`,
      };
    }
  }
}
