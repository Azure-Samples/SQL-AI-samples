# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
Read Data tool for MSSQL MCP Server.
"""

import logging
from typing import List, Dict, Any, Optional
from ..db_operation_result import DbOperationResult
from ..sql_connection_factory import ISqlConnectionFactory


class ReadDataTool:
    """
    Tool for reading data from the SQL Database.
    """
    
    def __init__(self, connection_factory: ISqlConnectionFactory, logger: logging.Logger):
        """
        Initialize the ReadDataTool.
        
        Args:
            connection_factory: Factory for creating database connections
            logger: Logger instance for error logging
        """
        self._connection_factory = connection_factory
        self._logger = logger
    
    @property
    def name(self) -> str:
        """Get the tool name."""
        return "read_data"
    
    @property
    def title(self) -> str:
        """Get the tool title."""
        return "Read Data"
    
    @property
    def description(self) -> str:
        """Get the tool description."""
        return "Executes SQL queries against SQL Database to read data"
    
    @property
    def readonly(self) -> bool:
        """Indicate if this tool is read-only."""
        return True
    
    @property
    def idempotent(self) -> bool:
        """Indicate if this tool is idempotent."""
        return True
    
    @property
    def destructive(self) -> bool:
        """Indicate if this tool is destructive."""
        return False
    
    async def execute(self, sql: str) -> DbOperationResult:
        """
        Execute the read data operation.
        
        Args:
            sql: SQL query to execute
            
        Returns:
            DbOperationResult containing the query results
        """
        try:
            connection = await self._connection_factory.get_open_connection()
            
            try:
                cursor = connection.cursor()
                cursor.execute(sql)
                
                # Get column names
                columns = [column[0] for column in cursor.description] if cursor.description else []
                
                # Fetch all rows
                rows = cursor.fetchall()
                
                # Convert rows to list of dictionaries
                results = []
                for row in rows:
                    row_dict = {}
                    for i, value in enumerate(row):
                        column_name = columns[i] if i < len(columns) else f"column_{i}"
                        row_dict[column_name] = value
                    results.append(row_dict)
                
                return DbOperationResult(success=True, data=results)
                
            finally:
                connection.close()
                
        except Exception as ex:
            self._logger.error(f"ReadData failed: {str(ex)}")
            return DbOperationResult(success=False, error=str(ex))
