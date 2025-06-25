import sql from "mssql";
import { Tool } from "@modelcontextprotocol/sdk/types.js";

export class ReadDataTool implements Tool {
  [key: string]: any;
  name = "read_data";
  description = "Executes a SELECT query on an MSSQL Database table. The query must start with SELECT and cannot contain any destructive SQL operations for security reasons.";
  
  inputSchema = {
    type: "object",
    properties: {
      query: { 
        type: "string", 
        description: "SQL SELECT query to execute (must start with SELECT and cannot contain destructive operations). Example: SELECT * FROM movies WHERE genre = 'comedy'" 
      },
    },
    required: ["query"],
  } as any;

  // List of dangerous SQL keywords that should not be allowed
  private static readonly DANGEROUS_KEYWORDS = [
    'DELETE', 'DROP', 'UPDATE', 'INSERT', 'ALTER', 'CREATE', 
    'TRUNCATE', 'EXEC', 'EXECUTE', 'MERGE', 'REPLACE',
    'GRANT', 'REVOKE', 'COMMIT', 'ROLLBACK', 'TRANSACTION',
    'BEGIN', 'DECLARE', 'SET', 'USE', 'BACKUP',
    'RESTORE', 'KILL', 'SHUTDOWN', 'WAITFOR', 'OPENROWSET',
    'OPENDATASOURCE', 'OPENQUERY', 'OPENXML', 'BULK'
  ];

  // Regex patterns to detect common SQL injection techniques
  private static readonly DANGEROUS_PATTERNS = [
    // Semicolon followed by dangerous keywords
    /;\s*(DELETE|DROP|UPDATE|INSERT|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE|MERGE|REPLACE|GRANT|REVOKE)/i,
    
    // UNION injection attempts with dangerous keywords
    /UNION\s+(?:ALL\s+)?SELECT.*?(DELETE|DROP|UPDATE|INSERT|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE)/i,
    
    // Comment-based injection attempts
    /--.*?(DELETE|DROP|UPDATE|INSERT|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE)/i,
    /\/\*.*?(DELETE|DROP|UPDATE|INSERT|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE).*?\*\//i,
    
    // Stored procedure execution patterns
    /EXEC\s*\(/i,
    /EXECUTE\s*\(/i,
    /sp_/i,
    /xp_/i,
    
    // Dynamic SQL construction
    /EXEC\s*\(/i,
    /EXECUTE\s*\(/i,
    
    // Bulk operations
    /BULK\s+INSERT/i,
    /OPENROWSET/i,
    /OPENDATASOURCE/i,
    
    // System functions that could be dangerous
    /@@/,
    /SYSTEM_USER/i,
    /USER_NAME/i,
    /DB_NAME/i,
    /HOST_NAME/i,
    
    // Time delay attacks
    /WAITFOR\s+DELAY/i,
    /WAITFOR\s+TIME/i,
    
    // Multiple statements (semicolon not at end)
    /;\s*\w/,
    
    // String concatenation that might hide malicious code
    /\+\s*CHAR\s*\(/i,
    /\+\s*NCHAR\s*\(/i,
    /\+\s*ASCII\s*\(/i,
  ];

  /**
   * Validates the SQL query for security issues
   * @param query The SQL query to validate
   * @returns Validation result with success flag and error message if invalid
   */
  private validateQuery(query: string): { isValid: boolean; error?: string } {
    if (!query || typeof query !== 'string') {
      return { 
        isValid: false, 
        error: 'Query must be a non-empty string' 
      };
    }

    // Remove comments and normalize whitespace for analysis
    const cleanQuery = query
      .replace(/--.*$/gm, '') // Remove line comments
      .replace(/\/\*[\s\S]*?\*\//g, '') // Remove block comments
      .replace(/\s+/g, ' ') // Normalize whitespace
      .trim();

    if (!cleanQuery) {
      return { 
        isValid: false, 
        error: 'Query cannot be empty after removing comments' 
      };
    }

    const upperQuery = cleanQuery.toUpperCase();

    // Must start with SELECT
    if (!upperQuery.startsWith('SELECT')) {
      return { 
        isValid: false, 
        error: 'Query must start with SELECT for security reasons' 
      };
    }

    // Check for dangerous keywords in the cleaned query using word boundaries
    for (const keyword of ReadDataTool.DANGEROUS_KEYWORDS) {
      // Use word boundary regex to match only complete keywords, not parts of words
      const keywordRegex = new RegExp(`(^|\\s|[^A-Za-z0-9_])${keyword}($|\\s|[^A-Za-z0-9_])`, 'i');
      if (keywordRegex.test(upperQuery)) {
        return { 
          isValid: false, 
          error: `Dangerous keyword '${keyword}' detected in query. Only SELECT operations are allowed.` 
        };
      }
    }

    // Check for dangerous patterns using regex
    for (const pattern of ReadDataTool.DANGEROUS_PATTERNS) {
      if (pattern.test(query)) {
        return { 
          isValid: false, 
          error: 'Potentially malicious SQL pattern detected. Only simple SELECT queries are allowed.' 
        };
      }
    }

    // Additional validation: Check for multiple statements
    const statements = cleanQuery.split(';').filter(stmt => stmt.trim().length > 0);
    if (statements.length > 1) {
      return { 
        isValid: false, 
        error: 'Multiple SQL statements are not allowed. Use only a single SELECT statement.' 
      };
    }

    // Check for suspicious string patterns that might indicate obfuscation
    if (query.includes('CHAR(') || query.includes('NCHAR(') || query.includes('ASCII(')) {
      return { 
        isValid: false, 
        error: 'Character conversion functions are not allowed as they may be used for obfuscation.' 
      };
    }

    // Limit query length to prevent potential DoS
    if (query.length > 10000) {
      return { 
        isValid: false, 
        error: 'Query is too long. Maximum allowed length is 10,000 characters.' 
      };
    }

    return { isValid: true };
  }

  /**
   * Sanitizes the query result to prevent any potential security issues
   * @param data The query result data
   * @returns Sanitized data
   */
  private sanitizeResult(data: any[]): any[] {
    if (!Array.isArray(data)) {
      return [];
    }

    // Limit the number of returned records to prevent memory issues
    const maxRecords = 10000;
    if (data.length > maxRecords) {
      console.warn(`Query returned ${data.length} records, limiting to ${maxRecords}`);
      return data.slice(0, maxRecords);
    }

    return data.map(record => {
      if (typeof record === 'object' && record !== null) {
        const sanitized: any = {};
        for (const [key, value] of Object.entries(record)) {
          // Sanitize column names (remove any suspicious characters)
          const sanitizedKey = key.replace(/[^\w\s-_.]/g, '');
          if (sanitizedKey !== key) {
            console.warn(`Column name sanitized: ${key} -> ${sanitizedKey}`);
          }
          sanitized[sanitizedKey] = value;
        }
        return sanitized;
      }
      return record;
    });
  }

  /**
   * Executes the validated SQL query
   * @param params Query parameters
   * @returns Query execution result
   */
  async run(params: any) {
    try {
      const { query } = params;
      
      // Validate the query for security issues
      const validation = this.validateQuery(query);
      if (!validation.isValid) {
        console.warn(`Security validation failed for query: ${query.substring(0, 100)}...`);
        return {
          success: false,
          message: `Security validation failed: ${validation.error}`,
          error: 'SECURITY_VALIDATION_FAILED'
        };
      }

      // Log the query for audit purposes (in production, consider more secure logging)
      console.log(`Executing validated SELECT query: ${query.substring(0, 200)}${query.length > 200 ? '...' : ''}`);

      // Execute the query
      const request = new sql.Request();
      const result = await request.query(query);
      
      // Sanitize the result
      const sanitizedData = this.sanitizeResult(result.recordset);
      
      return {
        success: true,
        message: `Query executed successfully. Retrieved ${sanitizedData.length} record(s)${
          result.recordset.length !== sanitizedData.length 
            ? ` (limited from ${result.recordset.length} total records)` 
            : ''
        }`,
        data: sanitizedData,
        recordCount: sanitizedData.length,
        totalRecords: result.recordset.length
      };
      
    } catch (error) {
      console.error("Error executing query:", error);
      
      // Don't expose internal error details to prevent information leakage
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      const safeErrorMessage = errorMessage.includes('Invalid object name') 
        ? errorMessage 
        : 'Database query execution failed';
      
      return {
        success: false,
        message: `Failed to execute query: ${safeErrorMessage}`,
        error: 'QUERY_EXECUTION_FAILED'
      };
    }
  }
}