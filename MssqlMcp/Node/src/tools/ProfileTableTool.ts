import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

const clampEnvInt = (value: string | undefined, fallback: number, min: number, max: number) => {
  if (!value) {
    return fallback;
  }
  const parsed = parseInt(value, 10);
  if (Number.isNaN(parsed)) {
    return fallback;
  }
  return Math.min(Math.max(parsed, min), max);
};

const DEFAULT_SAMPLE_SIZE = clampEnvInt(process.env.PROFILE_SAMPLE_SIZE_DEFAULT, 50, 1, 1000);
const SAMPLE_RETURN_LIMIT = clampEnvInt(process.env.PROFILE_SAMPLE_RETURN_LIMIT, 10, 1, 100);

type ProfileParams = {
  tableName: string;
  schemaName?: string;
  sampleSize?: number;
  includeDistributions?: boolean;
  topValuesLimit?: number;
  columnsToProfile?: string[];
  includeSamples?: boolean;
};

type NumericStats = {
  min: number;
  max: number;
  avg: number;
  median?: number;
  p90?: number;
};

type StringStats = {
  minLength: number;
  maxLength: number;
  avgLength: number;
  emptyCount: number;
};

type DateStats = {
  earliest: string;
  latest: string;
  range: string;
};

type TopValue = {
  value: any;
  count: number;
  percentage: number;
};

type ColumnProfile = {
  columnName: string;
  dataType: string;
  isNullable: boolean;
  nullCount: number;
  nullPercentage: number;
  distinctCount: number;
  cardinality: "unique" | "high" | "medium" | "low";
  numericStats?: NumericStats;
  stringStats?: StringStats;
  dateStats?: DateStats;
  topValues?: TopValue[];
};

type ProfileResult = {
  success: boolean;
  message?: string;
  tableName?: string;
  schemaName?: string;
  rowCount?: number;
  columnCount?: number;
  sampleSize?: number;
  columns?: ColumnProfile[];
  samples?: Record<string, unknown>[];
};

export class ProfileTableTool implements Tool {
  [key: string]: any;
  name = "profile_table";
  description =
    "Profiles a table by analyzing column statistics, data distributions, and sample records. Returns metadata, cardinality info, null counts, and representative samples for each column.";

  inputSchema = {
    type: "object",
    properties: {
      tableName: {
        type: "string",
        description: "Name of table to profile (schema.table or table)",
      },
      schemaName: {
        type: "string",
        description: "Explicit schema (defaults to 'dbo')",
      },
      sampleSize: {
        type: "number",
        description: "Number of sample rows (default 100, max 1000)",
      },
      includeSamples: {
        type: "boolean",
        description: "Return sampled rows used for profiling (default false)",
      },
      includeDistributions: {
        type: "boolean",
        description: "Include top value frequencies (default true)",
      },
      topValuesLimit: {
        type: "number",
        description: "Max distinct values per column (default 10, max 50)",
      },
      columnsToProfile: {
        type: "array",
        items: { type: "string" },
        description: "Specific columns to profile (default: all)",
      },
    },
    required: ["tableName"],
  } as any;

  // Binary/blob types to skip
  private static readonly SKIP_TYPES = [
    "image",
    "varbinary",
    "binary",
    "timestamp",
    "rowversion",
    "sql_variant",
    "xml",
    "geography",
    "geometry",
    "hierarchyid",
  ];

  private static readonly NUMERIC_TYPES = [
    "int",
    "bigint",
    "smallint",
    "tinyint",
    "decimal",
    "numeric",
    "float",
    "real",
    "money",
    "smallmoney",
  ];

  private static readonly STRING_TYPES = [
    "char",
    "varchar",
    "nchar",
    "nvarchar",
    "text",
    "ntext",
  ];

  private static readonly DATE_TYPES = [
    "date",
    "datetime",
    "datetime2",
    "smalldatetime",
    "datetimeoffset",
    "time",
  ];

  private normalizeLimit(value: number | undefined, defaultVal: number, max: number): number {
    if (typeof value !== "number" || Number.isNaN(value) || value <= 0) {
      return defaultVal;
    }
    return Math.min(Math.floor(value), max);
  }

  private classifyCardinality(distinctCount: number, rowCount: number): "unique" | "high" | "medium" | "low" {
    if (rowCount === 0) return "low";
    const ratio = distinctCount / rowCount;
    if (ratio > 0.95) return "unique";
    if (ratio > 0.5) return "high";
    if (ratio > 0.1) return "medium";
    return "low";
  }

  private formatDateRange(earliest: Date, latest: Date): string {
    const diffMs = latest.getTime() - earliest.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    
    if (diffDays < 1) return "less than 1 day";
    if (diffDays < 30) return `${diffDays} day${diffDays > 1 ? "s" : ""}`;
    
    const diffMonths = Math.floor(diffDays / 30);
    if (diffMonths < 12) return `${diffMonths} month${diffMonths > 1 ? "s" : ""}`;
    
    const years = Math.floor(diffMonths / 12);
    const remainingMonths = diffMonths % 12;
    if (remainingMonths === 0) return `${years} year${years > 1 ? "s" : ""}`;
    return `${years} year${years > 1 ? "s" : ""}, ${remainingMonths} month${remainingMonths > 1 ? "s" : ""}`;
  }

  private escapeIdentifier(name: string): string {
    return `[${name.replace(/\]/g, "]]")}]`;
  }

  private isNumericType(dataType: string): boolean {
    return ProfileTableTool.NUMERIC_TYPES.includes(dataType.toLowerCase());
  }

  private isStringType(dataType: string): boolean {
    return ProfileTableTool.STRING_TYPES.includes(dataType.toLowerCase());
  }

  private isDateType(dataType: string): boolean {
    return ProfileTableTool.DATE_TYPES.includes(dataType.toLowerCase());
  }

  private shouldSkipType(dataType: string): boolean {
    return ProfileTableTool.SKIP_TYPES.includes(dataType.toLowerCase());
  }

  async run(params: ProfileParams): Promise<ProfileResult> {
    try {
      const tableName = params.tableName?.trim();
      if (!tableName) {
        return { success: false, message: "tableName is required." };
      }

      const schemaName = params.schemaName?.trim() || "dbo";
      const sampleSize = this.normalizeLimit(params.sampleSize, DEFAULT_SAMPLE_SIZE, 1000);
      const includeDistributions = params.includeDistributions !== false;
      const includeSamples = params.includeSamples === true;
      const topValuesLimit = this.normalizeLimit(params.topValuesLimit, 10, 50);
      const columnsToProfile = params.columnsToProfile?.map((c) => c.trim()).filter(Boolean);

      // 1. Validate table exists and get columns
      const metaRequest = new sql.Request();
      metaRequest.input("schemaName", sql.NVarChar, schemaName);
      metaRequest.input("tableName", sql.NVarChar, tableName);

      const metaResult = await metaRequest.query(`
        SELECT 
          c.COLUMN_NAME AS columnName,
          c.DATA_TYPE AS dataType,
          CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS isNullable
        FROM INFORMATION_SCHEMA.COLUMNS c
        INNER JOIN INFORMATION_SCHEMA.TABLES t
          ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME
        WHERE c.TABLE_SCHEMA = @schemaName AND c.TABLE_NAME = @tableName
        ORDER BY c.ORDINAL_POSITION
      `);

      if (!metaResult.recordset.length) {
        return {
          success: false,
          message: `Table [${schemaName}].[${tableName}] not found or has no columns.`,
        };
      }

      // Filter columns if requested
      let columns: Array<{ columnName: string; dataType: string; isNullable: number }> = [...metaResult.recordset];
      if (columnsToProfile && columnsToProfile.length) {
        const requested = new Set(columnsToProfile.map((c) => c.toLowerCase()));
        columns = columns.filter((c) => requested.has(c.columnName.toLowerCase()));
        if (!columns.length) {
          return {
            success: false,
            message: `None of the requested columns exist in [${schemaName}].[${tableName}].`,
          };
        }
      }

      // Filter out binary/blob types
      columns = columns.filter((c) => !this.shouldSkipType(c.dataType));

      // 2. Get total row count
      const countRequest = new sql.Request();
      const fqTable = `${this.escapeIdentifier(schemaName)}.${this.escapeIdentifier(tableName)}`;
      const countResult = await countRequest.query(`SELECT COUNT(*) AS cnt FROM ${fqTable}`);
      const rowCount = countResult.recordset[0]?.cnt ?? 0;

      let sampleRows: Record<string, unknown>[] | undefined;

      if (rowCount === 0) {
        return {
          success: true,
          tableName,
          schemaName,
          rowCount: 0,
          columnCount: columns.length,
          sampleSize: 0,
          columns: columns.map((c) => ({
            columnName: c.columnName,
            dataType: c.dataType,
            isNullable: Boolean(c.isNullable),
            nullCount: 0,
            nullPercentage: 0,
            distinctCount: 0,
            cardinality: "low" as const,
          })),
        };
      }

      if (includeSamples) {
        const sampleRequest = new sql.Request();
        const sampleQuery = `
          SELECT TOP (${sampleSize}) *
          FROM ${fqTable}
          ORDER BY NEWID()
        `;
        const sampleResult = await sampleRequest.query(sampleQuery);
        sampleRows = (sampleResult.recordset ?? []).slice(0, SAMPLE_RETURN_LIMIT);
      }

      // 3. Profile each column
      const columnProfiles: ColumnProfile[] = [];

      for (const col of columns) {
        const colName = this.escapeIdentifier(col.columnName);
        const profile: ColumnProfile = {
          columnName: col.columnName,
          dataType: col.dataType,
          isNullable: Boolean(col.isNullable),
          nullCount: 0,
          nullPercentage: 0,
          distinctCount: 0,
          cardinality: "low",
        };

        // Base stats: null count and distinct count
        const baseRequest = new sql.Request();
        const baseResult = await baseRequest.query(`
          SELECT 
            SUM(CASE WHEN ${colName} IS NULL THEN 1 ELSE 0 END) AS nullCount,
            COUNT(DISTINCT ${colName}) AS distinctCount
          FROM ${fqTable}
        `);

        const baseRow = baseResult.recordset[0];
        profile.nullCount = baseRow?.nullCount ?? 0;
        profile.nullPercentage = rowCount > 0 ? Number(((profile.nullCount / rowCount) * 100).toFixed(2)) : 0;
        profile.distinctCount = baseRow?.distinctCount ?? 0;
        profile.cardinality = this.classifyCardinality(profile.distinctCount, rowCount);

        // Type-specific stats
        if (this.isNumericType(col.dataType)) {
          const numRequest = new sql.Request();
          const numResult = await numRequest.query(`
            SELECT 
              MIN(${colName}) AS minVal,
              MAX(${colName}) AS maxVal,
              AVG(CAST(${colName} AS FLOAT)) AS avgVal
            FROM ${fqTable}
            WHERE ${colName} IS NOT NULL
          `);
          const numRow = numResult.recordset[0];
          if (numRow && numRow.minVal !== null) {
            profile.numericStats = {
              min: numRow.minVal,
              max: numRow.maxVal,
              avg: Number(Number(numRow.avgVal).toFixed(4)),
            };

            const percentileRequest = new sql.Request();
            const percentileResult = await percentileRequest.query(`
              SELECT TOP 1
                PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY ${colName}) OVER () AS medianVal,
                PERCENTILE_CONT(0.9) WITHIN GROUP (ORDER BY ${colName}) OVER () AS p90Val
              FROM ${fqTable}
              WHERE ${colName} IS NOT NULL
            `);
            const percentileRow = percentileResult.recordset[0];
            if (percentileRow) {
              if (percentileRow.medianVal !== null && percentileRow.medianVal !== undefined) {
                profile.numericStats.median = Number(Number(percentileRow.medianVal).toFixed(4));
              }
              if (percentileRow.p90Val !== null && percentileRow.p90Val !== undefined) {
                profile.numericStats.p90 = Number(Number(percentileRow.p90Val).toFixed(4));
              }
            }
          }
        } else if (this.isStringType(col.dataType)) {
          const strRequest = new sql.Request();
          const strResult = await strRequest.query(`
            SELECT 
              MIN(LEN(${colName})) AS minLength,
              MAX(LEN(${colName})) AS maxLength,
              AVG(CAST(LEN(${colName}) AS FLOAT)) AS avgLength,
              SUM(CASE WHEN ${colName} = '' THEN 1 ELSE 0 END) AS emptyCount
            FROM ${fqTable}
            WHERE ${colName} IS NOT NULL
          `);
          const strRow = strResult.recordset[0];
          if (strRow && strRow.minLength !== null) {
            profile.stringStats = {
              minLength: strRow.minLength,
              maxLength: strRow.maxLength,
              avgLength: Number(Number(strRow.avgLength).toFixed(2)),
              emptyCount: strRow.emptyCount ?? 0,
            };
          }
        } else if (this.isDateType(col.dataType)) {
          const dateRequest = new sql.Request();
          const dateResult = await dateRequest.query(`
            SELECT 
              MIN(${colName}) AS earliest,
              MAX(${colName}) AS latest
            FROM ${fqTable}
            WHERE ${colName} IS NOT NULL
          `);
          const dateRow = dateResult.recordset[0];
          if (dateRow && dateRow.earliest !== null) {
            const earliest = new Date(dateRow.earliest);
            const latest = new Date(dateRow.latest);
            profile.dateStats = {
              earliest: earliest.toISOString(),
              latest: latest.toISOString(),
              range: this.formatDateRange(earliest, latest),
            };
          }
        }

        // Top values distribution
        if (includeDistributions && profile.distinctCount > 0 && profile.distinctCount <= rowCount) {
          const topRequest = new sql.Request();
          topRequest.input("topLimit", sql.Int, topValuesLimit);
          const topResult = await topRequest.query(`
            SELECT TOP (@topLimit)
              ${colName} AS value,
              COUNT(*) AS cnt
            FROM ${fqTable}
            WHERE ${colName} IS NOT NULL
            GROUP BY ${colName}
            ORDER BY cnt DESC
          `);

          if (topResult.recordset.length) {
            profile.topValues = topResult.recordset.map((r) => ({
              value: r.value,
              count: r.cnt,
              percentage: Number(((r.cnt / rowCount) * 100).toFixed(2)),
            }));
          }
        }

        columnProfiles.push(profile);
      }

      return {
        success: true,
        tableName,
        schemaName,
        rowCount,
        columnCount: columnProfiles.length,
        sampleSize: includeSamples ? sampleRows?.length ?? 0 : sampleSize,
        columns: columnProfiles,
        samples: includeSamples ? sampleRows : undefined,
      };
    } catch (error) {
      return {
        success: false,
        message: `Failed to profile table: ${error}`,
      };
    }
  }
}
