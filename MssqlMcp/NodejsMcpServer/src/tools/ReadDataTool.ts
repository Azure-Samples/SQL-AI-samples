import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

export class ReadDataTool implements Tool {
  [key: string]: any;
  name = "read_data";
  description = "Retrieves data from an MSSQL Database table by its ID";
  inputSchema = {
    type: "object",
    properties: {
      tableName: { type: "string", description: "Name of the table" },
      id: { type: "string", description: "ID of the data to retrieve" },
    },
    required: ["tableName", "id"],
  } as any;

  async run(params: any) {
    try {
      const { tableName, id } = params;
      const request = new sql.Request();
      request.input("id", sql.VarChar, id);
      const query = `SELECT * FROM ${tableName} WHERE id = @id`;
      const result = await request.query(query);
      return {
        success: true,
        message: `Data retrieved successfully`,
        data: result.recordset[0],
      };
    } catch (error) {
      console.error("Error retrieving data:", error);
      return {
        success: false,
        message: `Failed to retrieve data: ${error}`,
      };
    }
  }
}
