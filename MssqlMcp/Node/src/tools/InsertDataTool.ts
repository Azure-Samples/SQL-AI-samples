import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

export class InsertDataTool implements Tool {
  [key: string]: any;
  name = "insert_data";
  description = "Efficiently inserts data into an MSSQL Database table. For best performance, always use 'bulk' mode when inserting multiple rows. 'bulk' mode is highly preferred and should be used whenever possible; 'single' mode is only for inserting one row.";
  inputSchema = {
    type: "object",
    properties: {
      tableName: { type: "string", description: "Name of the table" },
      data: { type: ["object", "array"], description: "Data to insert into the table. Use an array of objects and 'bulk' mode for multiple rows (preferred); use a single object and 'single' mode only for one row." },
      mode: { type: "string", enum: ["single", "bulk"], default: "bulk", description: "Insert mode: 'bulk' (preferred, for multiple rows) or 'single' (only for one row). Always use 'bulk' when possible for better performance." },
    },
    required: ["tableName", "data"],
  } as any;

  /**
   * Example usage for bulk insert (preferred):
   *
   * await tool.run({
   *   tableName: "Users",
   *   data: [
   *     { name: "Alice", age: 30 },
   *     { name: "Bob", age: 25 }
   *   ],
   *   mode: "bulk"
   * });
   */

  async run(params: any) {
    try {
      const { tableName, data, mode = "bulk" } = params;
      if (mode === "bulk") {
        if (!Array.isArray(data) || data.length === 0) {
          throw new Error("For bulk mode, 'data' must be a non-empty array of objects.");
        }
        // Get columns from first row
        const columns = Object.keys(data[0]);
        // Create MSSQL Table object
        const table = new sql.Table(tableName);
        table.create = false; // Table already exists
        columns.forEach(col => table.columns.add(col, sql.NVarChar(sql.MAX), { nullable: true }));
        data.forEach(row => {
          table.rows.add(...columns.map(col => row[col]));
        });
        const request = new sql.Request();
        await request.bulk(table);
        return {
          success: true,
          message: `Bulk data added successfully to table`,
        };
      } else {
        // Single insert (only for one row)
        const columns = Object.keys(data).join(", ");
        const values = Object.keys(data)
          .map((key, index) => `@value${index}`)
          .join(", ");
        const request = new sql.Request();
        Object.entries(data).forEach(([key, value], index) => { 
          request.input(`value${index}`, value);
        });
        const query = `INSERT INTO ${tableName} (${columns}) VALUES (${values})`;
        await request.query(query);
        return {
          success: true,
          message: `Data added successfully to table`,
        };
      }
    } catch (error) {
      console.error("Error inserting data:", error);
      return {
        success: false,
        message: `Failed to insert data: ${error}`,
      };
    }
  }
}