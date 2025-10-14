# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
Create Table tool for MSSQL MCP Server.
"""

import logging
from ..db_operation_result import DbOperationResult
from ..sql_connection_factory import ISqlConnectionFactory


class CreateTableTool:
    """
    Tool for creating tables in the SQL Database.
    """
    
    def __init__(self, connection_factory: ISqlConnectionFactory, logger: logging.Logger):
        """
        Initialize the CreateTableTool.
        
        Args:
            connection_factory: Factory for creating database connections
            logger: Logger instance for error logging
        """
        self._connection_factory = connection_factory
        self._logger = logger
    
    @property
    def name(self) -> str:
        """Get the tool name."""
        return "create_table"
    
    @property
    def title(self) -> str:
        """Get the tool title."""
        return "Create Table"
    
    @property
    def description(self) -> str:
        """Get the tool description."""
        return "Creates a new table in the SQL Database. Expects a valid CREATE TABLE SQL statement as input."
    
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
        Execute the create table operation.
        
        Args:
            sql: CREATE TABLE SQL statement
            
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
            self._logger.error(f"CreateTable failed: {str(ex)}")
            return DbOperationResult(success=False, error=str(ex))
