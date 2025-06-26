import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

export class CreateIndexTool implements Tool {
  [key: string]: any;
  name = "create_index";
  description = "Creates an index on a specified column or columns in an MSSQL Database table";
  inputSchema = {
    type: "object",
    properties: {
      schemaName: { type: "string", description: "Name of the schema containing the table" },
      tableName: { type: "string", description: "Name of the table to create index on" },
      indexName: { type: "string", description: "Name for the new index" },
      columns: { 
        type: "array", 
        items: { type: "string" },
        description: "Array of column names to include in the index" 
      },
      isUnique: { 
        type: "boolean", 
        description: "Whether the index should enforce uniqueness (default: false)",
        default: false
      },
      isClustered: { 
        type: "boolean", 
        description: "Whether the index should be clustered (default: false)",
        default: false
      },
    },
    required: ["tableName", "indexName", "columns"],
  } as any;

  async run(params: any) {
    try {
      const { schemaName, tableName, indexName, columns, isUnique = false, isClustered = false } = params;

      let indexType = isClustered ? "CLUSTERED" : "NONCLUSTERED";
      if (isUnique) {
        indexType = `UNIQUE ${indexType}`;
      }
      const columnNames = columns.join(", ");

      const request = new sql.Request();
      const query = `CREATE ${indexType} INDEX ${indexName} ON ${schemaName}.${tableName} (${columnNames})`;
      await request.query(query);
      
      return {
        success: true,
        message: `Index [${indexName}] created successfully on table [${schemaName}.${tableName}]`,
        details: {
          schemaName,
          tableName,
          indexName,
          columnNames,
          isUnique,
          isClustered
        }
      };
    } catch (error) {
      console.error("Error creating index:", error);
      return {
        success: false,
        message: `Failed to create index: ${error}`,
      };
    }
  }
}