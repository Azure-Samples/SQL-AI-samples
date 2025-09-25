# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
List Tables tool for MSSQL MCP Server.
"""

import logging
from typing import List
from ..db_operation_result import DbOperationResult
from ..sql_connection_factory import ISqlConnectionFactory


class ListTablesTool:
    """
    Tool for listing all tables in the SQL Database.
    """
    
    def __init__(self, connection_factory: ISqlConnectionFactory, logger: logging.Logger):
        """
        Initialize the ListTablesTool.
        
        Args:
            connection_factory: Factory for creating database connections
            logger: Logger instance for error logging
        """
        self._connection_factory = connection_factory
        self._logger = logger
    
    @property
    def name(self) -> str:
        """Get the tool name."""
        return "list_tables"
    
    @property
    def title(self) -> str:
        """Get the tool title."""
        return "List Tables"
    
    @property
    def description(self) -> str:
        """Get the tool description."""
        return "Lists all tables in the SQL Database."
    
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
    
    async def execute(self) -> DbOperationResult:
        """
        Execute the list tables operation.
        
        Returns:
            DbOperationResult containing the list of tables
        """
        LIST_TABLES_QUERY = """
        SELECT TABLE_SCHEMA, TABLE_NAME 
        FROM INFORMATION_SCHEMA.TABLES 
        WHERE TABLE_TYPE = 'BASE TABLE' 
        ORDER BY TABLE_SCHEMA, TABLE_NAME
        """
        
        try:
            connection = await self._connection_factory.get_open_connection()
            
            try:
                cursor = connection.cursor()
                cursor.execute(LIST_TABLES_QUERY)
                
                tables = []
                for row in cursor.fetchall():
                    schema, table_name = row
                    tables.append(f"{schema}.{table_name}")
                
                return DbOperationResult(success=True, data=tables)
                
            finally:
                connection.close()
                
        except Exception as ex:
            self._logger.error(f"ListTables failed: {str(ex)}")
            return DbOperationResult(success=False, error=str(ex))
