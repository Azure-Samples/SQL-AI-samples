import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

export class InsertDataTool implements Tool {
  [key: string]: any;
  name = "insert_data";
  description = "Inserts or replaces data in an MSSQL Database table";
  inputSchema = {
    type: "object",
    properties: {
      tableName: { type: "string", description: "Name of the table" },
      data: { type: "object", description: "Data to insert into the table" },
    },
    required: ["tableName", "data"],
  } as any;

  async run(params: any) {
    try {
      const { tableName, data } = params;
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
    } catch (error) {
      console.error("Error inserting data:", error);
      return {
        success: false,
        message: `Failed to insert data: ${error}`,
      };
    }
  }
}
