import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

export class GetTableStatsTool implements Tool {
  [key: string]: any;
  name = "get_table_stats";
  description =
    "Returns row counts and space usage statistics for tables in the database.";
  inputSchema = {
    type: "object",
    properties: {
      tableName: {
        type: "string",
        description:
          "Table name to get stats for (supports 'schema.table' format). If omitted, returns stats for all tables.",
      },
    },
    required: [],
  } as any;

  async run(params: any) {
    try {
      const { tableName } = params || {};

      let tableNamePart: string | null = null;
      let schemaPart: string | null = null;

      if (tableName && typeof tableName === "string") {
        if (tableName.includes(".")) {
          const parts = tableName.split(".");
          schemaPart = parts[0];
          tableNamePart = parts[1];
        } else {
          tableNamePart = tableName;
        }
      }

      const request = new sql.Request();

      const query = `
        SELECT
            s.name AS [schema],
            t.name AS [table],
            SUM(p.rows) AS [rowCount],
            SUM(a.total_pages) * 8 AS totalSpaceKB,
            SUM(a.used_pages) * 8 AS usedSpaceKB
        FROM sys.tables t
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        INNER JOIN sys.indexes i ON t.object_id = i.object_id
        INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
        INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
        WHERE i.index_id <= 1
            AND (@TableName IS NULL OR t.name = @TableName)
            AND (@TableSchema IS NULL OR s.name = @TableSchema)
        GROUP BY s.name, t.name
        ORDER BY s.name, t.name`;

      request.input("TableName", sql.NVarChar, tableNamePart);
      request.input("TableSchema", sql.NVarChar, schemaPart);

      const result = await request.query(query);

      return {
        success: true,
        message: `Retrieved stats for ${result.recordset.length} table(s).`,
        data: result.recordset,
      };
    } catch (error) {
      console.error("Error getting table stats:", error);
      return {
        success: false,
        message: `Failed to get table stats: ${error}`,
      };
    }
  }
}
