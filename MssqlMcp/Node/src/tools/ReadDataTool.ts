import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

export class ReadDataTool implements Tool {
  [key: string]: any;
  name = "read_data";
  description = "Executes a SELECT query on an MSSQL Database table. The query must start with SELECT for security.";
  inputSchema = {
    type: "object",
    properties: {
      query: { 
        type: "string", 
        description: "SQL SELECT query to execute (must start with SELECT). Example: SELECT * FROM movies WHERE genre = 'comedy'" 
      },
    },
    required: ["query"],
  } as any;

  async run(params: any) {
    try {
      const { query } = params;
      
      // Basic validation: ensure query starts with SELECT (case insensitive)
      const trimmedQuery = query.trim();
      if (!trimmedQuery.toUpperCase().startsWith('SELECT')) {
        throw new Error("Query must start with SELECT for security reasons");
      }

      const request = new sql.Request();
      const result = await request.query(trimmedQuery);
      
      return {
        success: true,
        message: `Query executed successfully. Retrieved ${result.recordset.length} record(s)`,
        data: result.recordset,
        recordCount: result.recordset.length,
      };
    } catch (error) {
      console.error("Error executing query:", error);
      return {
        success: false,
        message: `Failed to execute query: ${error}`,
      };
    }
  }
}
