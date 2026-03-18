import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";
import type { ConnectionManager } from "../ConnectionManager.js";

export class CreateIndexTool implements Tool {
  [key: string]: any;
  name = "create_index";
  description = "Creates an index on a specified column or columns in an MSSQL Database table";

  private manager: ConnectionManager;
  constructor(manager: ConnectionManager) {
    this.manager = manager;
  }

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
      database: {
        type: "string",
        description: "Target database name. If omitted, uses the last selected database. Use list_databases to discover available databases.",
      },
    },
    required: ["tableName", "indexName", "columns"],
  } as any;

  async run(params: any) {
    try {
      const { schemaName, tableName, indexName, columns, isUnique = false, isClustered = false } = params;
      const schema = schemaName || "dbo";

      let indexType = isClustered ? "CLUSTERED" : "NONCLUSTERED";
      if (isUnique) {
        indexType = `UNIQUE ${indexType}`;
      }
      const columnNames = columns.join(", ");

      const pool = await this.manager.resolveConnection(params.database);
      const request = pool.request();
      const query = `CREATE ${indexType} INDEX ${indexName} ON ${schema}.${tableName} (${columnNames})`;
      await request.query(query);
      
      return {
        success: true,
        message: `Index [${indexName}] created successfully on table [${schema}.${tableName}]`,
        details: {
          schemaName: schema,
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