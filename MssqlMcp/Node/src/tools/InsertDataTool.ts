import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";
export class InsertDataTool implements Tool {
  [key: string]: any;
  name = "insert_data";
  description = `Inserts data into an MSSQL Database table. Supports both single record insertion and multiple record insertion using standard SQL INSERT with VALUES clause.
FORMAT EXAMPLES:
Single Record Insert:
{
  "tableName": "Users",
  "data": {
    "name": "John Doe",
    "email": "john@example.com",
    "age": 30,
    "isActive": true,
    "createdDate": "2023-01-15"
  }
}
Multiple Records Insert:
{
  "tableName": "Users", 
  "data": [
    {
      "name": "John Doe",
      "email": "john@example.com", 
      "age": 30,
      "isActive": true,
      "createdDate": "2023-01-15"
    },
    {
      "name": "Jane Smith",
      "email": "jane@example.com",
      "age": 25, 
      "isActive": false,
      "createdDate": "2023-01-16"
    }
  ]
}
GENERATED SQL FORMAT:
- Single: INSERT INTO table (col1, col2) VALUES (@param1, @param2)
- Multiple: INSERT INTO table (col1, col2) VALUES (@param1, @param2), (@param3, @param4), ...
IMPORTANT RULES:
- For single record: Use a single object for the 'data' field
- For multiple records: Use an array of objects for the 'data' field
- All objects in array must have identical column names
- Column names must match the actual database table columns exactly
- Values should match the expected data types (string, number, boolean, date)
- Use proper date format for date columns (YYYY-MM-DD or ISO format)`;
  inputSchema = {
    type: "object",
    properties: {
      tableName: { 
        type: "string", 
        description: "Name of the table to insert data into" 
      },
      data: { 
        oneOf: [
          { 
            type: "object", 
            description: "Single record data object with column names as keys and values as the data to insert. Example: {\"name\": \"John\", \"age\": 30}" 
          },
          { 
            type: "array", 
            items: { type: "object" },
            description: "Array of data objects for multiple record insertion. Each object must have identical column structure. Example: [{\"name\": \"John\", \"age\": 30}, {\"name\": \"Jane\", \"age\": 25}]" 
          }
        ]
      },
    },
    required: ["tableName", "data"],
  } as any;
  async run(params: any) {
    try {
      const { tableName, data } = params;
      // Check if data is an array (multiple records) or single object
      const isMultipleRecords = Array.isArray(data);
      const records = isMultipleRecords ? data : [data];
      if (records.length === 0) {
        return {
          success: false,
          message: "No data provided for insertion",
        };
      }
      // Validate that all records have the same columns
      const firstRecordColumns = Object.keys(records[0]).sort();
      for (let i = 1; i < records.length; i++) {
        const currentColumns = Object.keys(records[i]).sort();
        if (JSON.stringify(firstRecordColumns) !== JSON.stringify(currentColumns)) {
          return {
            success: false,
            message: `Column mismatch: Record ${i + 1} has different columns than the first record. Expected columns: [${firstRecordColumns.join(', ')}], but got: [${currentColumns.join(', ')}]`,
          };
        }
      }
      const columns = firstRecordColumns.join(", ");
      const request = new sql.Request();
      if (isMultipleRecords) {
        // Multiple records insert using VALUES clause - works for 1 or more records
        const valueClauses: string[] = [];
        records.forEach((record, recordIndex) => {
          const valueParams = firstRecordColumns
            .map((column, columnIndex) => `@value${recordIndex}_${columnIndex}`)
            .join(", ");
          valueClauses.push(`(${valueParams})`);
          // Add parameters for this record
          firstRecordColumns.forEach((column, columnIndex) => {
            request.input(`value${recordIndex}_${columnIndex}`, record[column]);
          });
        });
        const query = `INSERT INTO ${tableName} (${columns}) VALUES ${valueClauses.join(", ")}`;
        await request.query(query);
        return {
          success: true,
          message: `Successfully inserted ${records.length} record${records.length > 1 ? 's' : ''} into ${tableName}`,
          recordsInserted: records.length,
        };
      } else {
        // Single record insert (when data is passed as single object)
        const values = firstRecordColumns
          .map((column, index) => `@value${index}`)
          .join(", ");
        firstRecordColumns.forEach((column, index) => {
          request.input(`value${index}`, records[0][column]);
        });
        const query = `INSERT INTO ${tableName} (${columns}) VALUES (${values})`;
        await request.query(query);
        return {
          success: true,
          message: `Successfully inserted 1 record into ${tableName}`,
          recordsInserted: 1,
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