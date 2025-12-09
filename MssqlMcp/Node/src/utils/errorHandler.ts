/**
 * Error handling utilities for MSSQL MCP Server
 * Provides configurable error verbosity for debugging
 */

export interface DetailedError {
  success: false;
  message: string;
  error: string;
  details?: {
    code?: string | number;
    number?: number;
    state?: any;
    class?: number;
    lineNumber?: number;
    serverName?: string;
    procName?: string;
    stack?: string;
    originalError?: any;
  };
}

/**
 * Check if verbose error mode is enabled
 * Set environment variable: VERBOSE_ERRORS=true
 */
export function isVerboseErrorMode(): boolean {
  return process.env.VERBOSE_ERRORS?.toLowerCase() === 'true';
}

/**
 * Format error for MCP response with optional verbose details
 * @param error The error object
 * @param context Context message (e.g., "executing query", "connecting to database")
 * @param errorCode Error code for the error type
 * @returns Formatted error object
 */
export function formatError(
  error: unknown,
  context: string,
  errorCode: string = 'ERROR'
): DetailedError {
  const verbose = isVerboseErrorMode();

  // Extract basic error message
  let message = 'Unknown error occurred';
  let details: DetailedError['details'] = undefined;

  if (error instanceof Error) {
    message = error.message;

    if (verbose) {
      details = {
        stack: error.stack
      };

      // Extract SQL-specific error properties if available
      const sqlError = error as any;
      if (sqlError.number !== undefined) details.number = sqlError.number;
      if (sqlError.code !== undefined) details.code = sqlError.code;
      if (sqlError.state !== undefined) details.state = sqlError.state;
      if (sqlError.class !== undefined) details.class = sqlError.class;
      if (sqlError.lineNumber !== undefined) details.lineNumber = sqlError.lineNumber;
      if (sqlError.serverName !== undefined) details.serverName = sqlError.serverName;
      if (sqlError.procName !== undefined) details.procName = sqlError.procName;
      if (sqlError.originalError !== undefined) {
        details.originalError = sqlError.originalError instanceof Error
          ? sqlError.originalError.message
          : String(sqlError.originalError);
      }
    }
  } else if (typeof error === 'string') {
    message = error;
  } else if (error && typeof error === 'object') {
    message = JSON.stringify(error);
    if (verbose) {
      details = { originalError: error };
    }
  }

  // Build formatted error response
  const result: DetailedError = {
    success: false,
    message: `Failed to ${context}: ${message}`,
    error: errorCode
  };

  if (verbose && details && Object.keys(details).length > 0) {
    result.details = details;
  }

  // Log error for debugging (always logged to console)
  console.error(`[${errorCode}] Error ${context}:`, error);
  if (verbose && details) {
    console.error('Error details:', JSON.stringify(details, null, 2));
  }

  return result;
}

/**
 * Format SQL error with enhanced debugging information
 * @param error SQL error object
 * @param query The SQL query that failed (optional, will be truncated)
 * @returns Formatted error object
 */
export function formatSqlError(
  error: unknown,
  query?: string
): DetailedError {
  const verbose = isVerboseErrorMode();
  const result = formatError(error, 'execute query', 'SQL_EXECUTION_FAILED');

  // Add query context if in verbose mode
  if (verbose && query && result.details) {
    result.details.originalError = {
      query: query.length > 500 ? query.substring(0, 500) + '...' : query,
      ...(typeof result.details.originalError === 'object' ? result.details.originalError : {})
    };
  }

  return result;
}

/**
 * Safe error message for production (hides sensitive details)
 * Used when VERBOSE_ERRORS is not enabled
 * @param error The error object
 * @param allowedPatterns Patterns that are safe to show (e.g., "Invalid object name")
 * @returns Safe error message
 */
export function getSafeErrorMessage(
  error: unknown,
  allowedPatterns: string[] = ['Invalid object name', 'Invalid column name']
): string {
  if (isVerboseErrorMode()) {
    // In verbose mode, show everything
    if (error instanceof Error) {
      return error.message;
    }
    return String(error);
  }

  // In production mode, only show allowed patterns
  const message = error instanceof Error ? error.message : String(error);

  for (const pattern of allowedPatterns) {
    if (message.includes(pattern)) {
      return message;
    }
  }

  // Hide error details for security
  return 'Database operation failed. Enable VERBOSE_ERRORS=true for details.';
}
