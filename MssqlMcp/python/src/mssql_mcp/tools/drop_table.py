# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
Drop Table tool for MSSQL MCP Server.
"""

import logging
from ..db_operation_result import DbOperationResult
from ..sql_connection_factory import ISqlConnectionFactory


class DropTableTool:
    """
    Tool for dropping tables in the SQL Database.
    """
    
    def __init__(self, connection_factory: ISqlConnectionFactory, logger: logging.Logger):
        """
        Initialize the DropTableTool.
        
        Args:
            connection_factory: Factory for creating database connections
            logger: Logger instance for error logging
        """
        self._connection_factory = connection_factory
        self._logger = logger
    
    @property
    def name(self) -> str:
        """Get the tool name."""
        return "drop_table"
    
    @property
    def title(self) -> str:
        """Get the tool title."""
        return "Drop Table"
    
    @property
    def description(self) -> str:
        """Get the tool description."""
        return "Drops a table in the SQL Database. Expects a valid DROP TABLE SQL statement as input."
    
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
        Execute the drop table operation.
        
        Args:
            sql: DROP TABLE SQL statement
            
        Returns:
            DbOperationResult indicating success or failure
        """
        try:
            connection = await self._connection_factory.get_open_connection()
            
            try:
                cursor = connection.cursor()
                cursor.execute(sql)
                connection.commit()
                
                return DbOperationResult(success=True)
                
            finally:
                connection.close()
                
        except Exception as ex:
            self._logger.error(f"DropTable failed: {str(ex)}")
            return DbOperationResult(success=False, error=str(ex))
