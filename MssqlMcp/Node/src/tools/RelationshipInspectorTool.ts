import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

type RelationshipParams = {
  tableName: string;
  schemaName?: string;
  includeOutbound?: boolean;
  includeInbound?: boolean;
};

type ColumnMapping = {
  fromColumn: string;
  toColumn: string;
};

type Relationship = {
  constraintName: string;
  from: { schemaName: string; tableName: string };
  to: { schemaName: string; tableName: string };
  columnMapping: ColumnMapping[];
  updateRule: string;
  deleteRule: string;
};

type RelationshipResult = {
  success: boolean;
  message?: string;
  tableName?: string;
  schemaName?: string;
  outbound?: Relationship[];
  inbound?: Relationship[];
};

type RelationshipRow = {
  constraintName: string;
  updateRule: string;
  deleteRule: string;
  fromSchema: string;
  fromTable: string;
  toSchema: string;
  toTable: string;
  fromColumn: string;
  toColumn: string;
  ordinal: number;
};

export class RelationshipInspectorTool implements Tool {
  [key: string]: any;
  name = "inspect_relationships";
  description = "Describes inbound and outbound foreign-key relationships for a given table, including column mappings and referential actions.";

  inputSchema = {
    type: "object",
    properties: {
      tableName: {
        type: "string",
        description: "Target table name (without schema).",
      },
      schemaName: {
        type: "string",
        description: "Schema name (defaults to 'dbo').",
      },
      includeOutbound: {
        type: "boolean",
        description: "Include relationships where the table references other tables (default true).",
      },
      includeInbound: {
        type: "boolean",
        description: "Include relationships where other tables reference this table (default true).",
      },
    },
    required: ["tableName"],
  } as any;

  private normalizeBool(value: boolean | undefined, fallback: boolean) {
    return typeof value === "boolean" ? value : fallback;
  }

  private async ensureTableExists(schemaName: string, tableName: string) {
    const request = new sql.Request();
    request.input("schemaName", sql.NVarChar, schemaName);
    request.input("tableName", sql.NVarChar, tableName);
    const result = await request.query(`
      SELECT 1
      FROM INFORMATION_SCHEMA.TABLES
      WHERE TABLE_SCHEMA = @schemaName AND TABLE_NAME = @tableName
    `);
    return result.recordset.length > 0;
  }

  private mapRelationships(rows: RelationshipRow[]): Relationship[] {
    const grouped = new Map<string, Relationship>();
    for (const row of rows) {
      if (!grouped.has(row.constraintName)) {
        grouped.set(row.constraintName, {
          constraintName: row.constraintName,
          from: { schemaName: row.fromSchema, tableName: row.fromTable },
          to: { schemaName: row.toSchema, tableName: row.toTable },
          columnMapping: [],
          updateRule: row.updateRule,
          deleteRule: row.deleteRule,
        });
      }
      const relationship = grouped.get(row.constraintName)!;
      relationship.columnMapping.push({ fromColumn: row.fromColumn, toColumn: row.toColumn });
    }

    return Array.from(grouped.values()).map((rel) => ({
      ...rel,
      columnMapping: rel.columnMapping.sort((a, b) => a.fromColumn.localeCompare(b.fromColumn)),
    }));
  }

  private async fetchRelationships(schemaName: string, tableName: string, direction: "outbound" | "inbound") {
    const request = new sql.Request();
    request.input("schemaName", sql.NVarChar, schemaName);
    request.input("tableName", sql.NVarChar, tableName);

    const baseQuery = `
      SELECT
        fk.CONSTRAINT_NAME AS constraintName,
        fk.UPDATE_RULE AS updateRule,
        fk.DELETE_RULE AS deleteRule,
        child.TABLE_SCHEMA AS fromSchema,
        child.TABLE_NAME AS fromTable,
        parent.TABLE_SCHEMA AS toSchema,
        parent.TABLE_NAME AS toTable,
        childCol.COLUMN_NAME AS fromColumn,
        parentCol.COLUMN_NAME AS toColumn,
        childCol.ORDINAL_POSITION AS ordinal
      FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS fk
      JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS child
        ON child.CONSTRAINT_NAME = fk.CONSTRAINT_NAME
        AND child.CONSTRAINT_TYPE = 'FOREIGN KEY'
      JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS parent
        ON parent.CONSTRAINT_NAME = fk.UNIQUE_CONSTRAINT_NAME
      JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE childCol
        ON childCol.CONSTRAINT_NAME = fk.CONSTRAINT_NAME
        AND childCol.TABLE_SCHEMA = child.TABLE_SCHEMA
        AND childCol.TABLE_NAME = child.TABLE_NAME
      JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE parentCol
        ON parentCol.CONSTRAINT_NAME = fk.UNIQUE_CONSTRAINT_NAME
        AND parentCol.TABLE_SCHEMA = parent.TABLE_SCHEMA
        AND parentCol.TABLE_NAME = parent.TABLE_NAME
        AND childCol.ORDINAL_POSITION = parentCol.ORDINAL_POSITION
      WHERE {{FILTER}}
      ORDER BY fk.CONSTRAINT_NAME, childCol.ORDINAL_POSITION
    `;

    const filter = direction === "outbound"
      ? "child.TABLE_SCHEMA = @schemaName AND child.TABLE_NAME = @tableName"
      : "parent.TABLE_SCHEMA = @schemaName AND parent.TABLE_NAME = @tableName";

    const query = baseQuery.replace("{{FILTER}}", filter);
    const result = await request.query<RelationshipRow>(query);
    return this.mapRelationships(result.recordset ?? []);
  }

  async run(params: RelationshipParams): Promise<RelationshipResult> {
    try {
      const tableName = params.tableName?.trim();
      if (!tableName) {
        return { success: false, message: "tableName is required." };
      }

      const schemaName = params.schemaName?.trim() || "dbo";
      const includeOutbound = this.normalizeBool(params.includeOutbound, true);
      const includeInbound = this.normalizeBool(params.includeInbound, true);

      if (!includeOutbound && !includeInbound) {
        return {
          success: false,
          message: "At least one of includeOutbound or includeInbound must be true.",
        };
      }

      const tableExists = await this.ensureTableExists(schemaName, tableName);
      if (!tableExists) {
        return {
          success: false,
          message: `Table [${schemaName}].[${tableName}] was not found.`,
        };
      }

      const [outbound, inbound] = await Promise.all([
        includeOutbound ? this.fetchRelationships(schemaName, tableName, "outbound") : Promise.resolve([]),
        includeInbound ? this.fetchRelationships(schemaName, tableName, "inbound") : Promise.resolve([]),
      ]);

      if ((!outbound || outbound.length === 0) && (!inbound || inbound.length === 0)) {
        return {
          success: true,
          tableName,
          schemaName,
          message: "No foreign key relationships found for the specified table.",
        };
      }

      return {
        success: true,
        tableName,
        schemaName,
        outbound: outbound.length ? outbound : undefined,
        inbound: inbound.length ? inbound : undefined,
      };
    } catch (error) {
      return {
        success: false,
        message: `Failed to inspect relationships: ${error}`,
      };
    }
  }
}
