# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
Insert Data tool for MSSQL MCP Server.
"""

import logging
from ..db_operation_result import DbOperationResult
from ..sql_connection_factory import ISqlConnectionFactory


class InsertDataTool:
    """
    Tool for inserting data into the SQL Database.
    """
    
    def __init__(self, connection_factory: ISqlConnectionFactory, logger: logging.Logger):
        """
        Initialize the InsertDataTool.
        
        Args:
            connection_factory: Factory for creating database connections
            logger: Logger instance for error logging
        """
        self._connection_factory = connection_factory
        self._logger = logger
    
    @property
    def name(self) -> str:
        """Get the tool name."""
        return "insert_data"
    
    @property
    def title(self) -> str:
        """Get the tool title."""
        return "Insert Data"
    
    @property
    def description(self) -> str:
        """Get the tool description."""
        return "Inserts data into a table in the SQL Database. Expects a valid INSERT SQL statement as input."
    
    @property
    def readonly(self) -> bool:
        """Indicate if this tool is read-only."""
        return False
    
    @property
    def idempotent(self) -> bool:
        """Indicate if this tool is idempotent."""
        return False
    
    @property
    def destructive(self) -> bool:
        """Indicate if this tool is destructive."""
        return False
    
    async def execute(self, sql: str) -> DbOperationResult:
        """
        Execute the insert data operation.
        
        Args:
            sql: INSERT SQL statement
            
        Returns:
            DbOperationResult indicating success or failure with rows affected
        """
        try:
            connection = await self._connection_factory.get_open_connection()
            
            try:
                cursor = connection.cursor()
                rows_affected = cursor.execute(sql).rowcount
                connection.commit()
                
                return DbOperationResult(success=True, rows_affected=rows_affected)
                
            finally:
                connection.close()
                
        except Exception as ex:
            self._logger.error(f"InsertData failed: {str(ex)}")
            return DbOperationResult(success=False, error=str(ex))
