import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

export class UpdateDataTool implements Tool {
  [key: string]: any;
  name = "update_data";
  description = "Updates data in an Azure SQL Database table by its ID";
  inputSchema = {
    type: "object",
    properties: {
      tableName: { type: "string", description: "Name of the table" },
      id: { type: "string", description: "ID of the data to update" },
      updates: {
        type: "object",
        description: "Key-value pairs of columns to update",
      },
    },
    required: ["tableName", "id", "updates"],
  } as any;

  async run(params: any) {
    try {
      const { tableName, id, updates } = params;
      const request = new sql.Request();
      request.input("id", sql.VarChar, id);
      const setClause = Object.keys(updates)
        .map((key) => {
          request.input(key, updates[key]);
          return `${key} = @${key}`;
        })
        .join(", ");
      const query = `UPDATE ${tableName} SET ${setClause} WHERE id = @id`;
      await request.query(query);
      return {
        success: true,
        message: `Data updated successfully`,
      };
    } catch (error) {
      console.error("Error updating data:", error);
      return {
        success: false,
        message: `Failed to update data: ${error}`,
      };
    }
  }
}
