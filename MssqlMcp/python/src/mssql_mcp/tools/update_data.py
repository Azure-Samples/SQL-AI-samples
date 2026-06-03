# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
Update Data tool for MSSQL MCP Server.
"""

import logging
from ..db_operation_result import DbOperationResult
from ..sql_connection_factory import ISqlConnectionFactory


class UpdateDataTool:
    """
    Tool for updating data in the SQL Database.
    """
    
    def __init__(self, connection_factory: ISqlConnectionFactory, logger: logging.Logger):
        """
        Initialize the UpdateDataTool.
        
        Args:
            connection_factory: Factory for creating database connections
            logger: Logger instance for error logging
        """
        self._connection_factory = connection_factory
        self._logger = logger
    
    @property
    def name(self) -> str:
        """Get the tool name."""
        return "update_data"
    
    @property
    def title(self) -> str:
        """Get the tool title."""
        return "Update Data"
    
    @property
    def description(self) -> str:
        """Get the tool description."""
        return "Updates data in a table in the SQL Database. Expects a valid UPDATE SQL statement as input."
    
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
        return True
    
    async def execute(self, sql: str) -> DbOperationResult:
        """
        Execute the update data operation.
        
        Args:
            sql: UPDATE SQL statement
            
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
            self._logger.error(f"UpdateData failed: {str(ex)}")
            return DbOperationResult(success=False, error=str(ex))
