import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

export class UpdateDataTool implements Tool {
  [key: string]: any;
  name = "update_data";
  description = "Updates data in an MSSQL Database table using a WHERE clause. The WHERE clause must be provided for security.";
  inputSchema = {
    type: "object",
    properties: {
      tableName: { 
        type: "string", 
        description: "Name of the table to update" 
      },
      updates: {
        type: "object",
        description: "Key-value pairs of columns to update. Example: { 'status': 'active', 'last_updated': '2025-01-01' }",
      },
      whereClause: { 
        type: "string", 
        description: "WHERE clause to identify which records to update. Example: \"genre = 'comedy' AND created_date <= '2025-07-05'\"" 
      },
    },
    required: ["tableName", "updates", "whereClause"],
  } as any;

  async run(params: any) {
    let query: string | undefined;
    try {
      const { tableName, updates, whereClause } = params;
      
      // Basic validation: ensure whereClause is not empty
      if (!whereClause || whereClause.trim() === '') {
        throw new Error("WHERE clause is required for security reasons");
      }

      const request = new sql.Request();
      
      // Build SET clause with parameterized queries for security
      const setClause = Object.keys(updates)
        .map((key, index) => {
          const paramName = `update_${index}`;
          request.input(paramName, updates[key]);
          return `[${key}] = @${paramName}`;
        })
        .join(", ");

      query = `UPDATE ${tableName} SET ${setClause} WHERE ${whereClause}`;
      const result = await request.query(query);
      
      return {
        success: true,
        message: `Update completed successfully. ${result.rowsAffected[0]} row(s) affected`,
        rowsAffected: result.rowsAffected[0],
      };
    } catch (error) {
      console.error("Error updating data:", error);
      return {
        success: false,
        message: `Failed to update data ${query ? ` with '${query}'` : ''}: ${error}`,
      };
    }
  }
}
