# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
Describe Table tool for MSSQL MCP Server.
"""

import logging
from typing import Dict, Any, List, Optional
from ..db_operation_result import DbOperationResult
from ..sql_connection_factory import ISqlConnectionFactory


class DescribeTableTool:
    """
    Tool for describing table schema in the SQL Database.
    """
    
    def __init__(self, connection_factory: ISqlConnectionFactory, logger: logging.Logger):
        """
        Initialize the DescribeTableTool.
        
        Args:
            connection_factory: Factory for creating database connections
            logger: Logger instance for error logging
        """
        self._connection_factory = connection_factory
        self._logger = logger
    
    @property
    def name(self) -> str:
        """Get the tool name."""
        return "describe_table"
    
    @property
    def title(self) -> str:
        """Get the tool title."""
        return "Describe Table"
    
    @property
    def description(self) -> str:
        """Get the tool description."""
        return "Returns table schema"
    
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
    
    async def execute(self, table_name: str) -> DbOperationResult:
        """
        Execute the describe table operation.
        
        Args:
            table_name: Name of the table to describe (can include schema.table format)
            
        Returns:
            DbOperationResult containing the table schema information
        """
        # Parse schema and table name
        schema = None
        if '.' in table_name:
            parts = table_name.split('.')
            if len(parts) > 1:
                schema = parts[0]
                table_name = parts[1]
        
        # Query for table metadata
        TABLE_INFO_QUERY = """
        SELECT t.object_id AS id, t.name, s.name AS [schema], p.value AS description, t.type, u.name AS owner
        FROM sys.tables t
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        LEFT JOIN sys.extended_properties p ON p.major_id = t.object_id AND p.minor_id = 0 AND p.name = 'MS_Description'
        LEFT JOIN sys.sysusers u ON t.principal_id = u.uid
        WHERE t.name = ? AND (s.name = ? OR ? IS NULL)
        """
        
        # Query for columns
        COLUMNS_QUERY = """
        SELECT c.name, ty.name AS type, c.max_length AS length, c.precision, c.is_nullable AS nullable, p.value AS description
        FROM sys.columns c
        INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
        LEFT JOIN sys.extended_properties p ON p.major_id = c.object_id AND p.minor_id = c.column_id AND p.name = 'MS_Description'
        WHERE c.object_id = (
            SELECT object_id FROM sys.tables t 
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id 
            WHERE t.name = ? AND (s.name = ? OR ? IS NULL)
        )
        """
        
        # Query for indexes
        INDEXES_QUERY = """
        SELECT i.name, i.type_desc AS type, p.value AS description,
            STUFF((
                SELECT ',' + c.name 
                FROM sys.index_columns ic
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id 
                ORDER BY ic.key_ordinal 
                FOR XML PATH('')
            ), 1, 1, '') AS keys
        FROM sys.indexes i
        LEFT JOIN sys.extended_properties p ON p.major_id = i.object_id AND p.minor_id = i.index_id AND p.name = 'MS_Description'
        WHERE i.object_id = (
            SELECT object_id FROM sys.tables t 
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id 
            WHERE t.name = ? AND (s.name = ? OR ? IS NULL)
        ) AND i.is_primary_key = 0 AND i.is_unique_constraint = 0
        """
        
        # Query for constraints
        CONSTRAINTS_QUERY = """
        SELECT kc.name, kc.type_desc AS type,
            STUFF((
                SELECT ',' + c.name 
                FROM sys.index_columns ic
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id 
                ORDER BY ic.key_ordinal 
                FOR XML PATH('')
            ), 1, 1, '') AS keys
        FROM sys.key_constraints kc
        WHERE kc.parent_object_id = (
            SELECT object_id FROM sys.tables t 
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id 
            WHERE t.name = ? AND (s.name = ? OR ? IS NULL)
        )
        """
        
        # Query for foreign keys
        FOREIGN_KEY_QUERY = """
        SELECT
            fk.name AS name,
            SCHEMA_NAME(tp.schema_id) AS [schema],
            tp.name AS table_name,
            STRING_AGG(cp.name, ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS column_names,
            SCHEMA_NAME(tr.schema_id) AS referenced_schema,
            tr.name AS referenced_table,
            STRING_AGG(cr.name, ', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS referenced_column_names
        FROM sys.foreign_keys AS fk
        JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
        JOIN sys.tables AS tp ON fkc.parent_object_id = tp.object_id
        JOIN sys.columns AS cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
        JOIN sys.tables AS tr ON fkc.referenced_object_id = tr.object_id
        JOIN sys.columns AS cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
        WHERE (SCHEMA_NAME(tp.schema_id) = ? OR ? IS NULL) AND tp.name = ?
        GROUP BY fk.name, tp.schema_id, tp.name, tr.schema_id, tr.name
        """
        
        try:
            connection = await self._connection_factory.get_open_connection()
            
            try:
                cursor = connection.cursor()
                result: Dict[str, Any] = {}
                
                # Get table info
                cursor.execute(TABLE_INFO_QUERY, (table_name, schema, schema))
                table_row = cursor.fetchone()
                
                if not table_row:
                    return DbOperationResult(success=False, error=f"Table '{table_name}' not found.")
                
                result["table"] = {
                    "id": table_row[0],
                    "name": table_row[1],
                    "schema": table_row[2],
                    "owner": table_row[5],
                    "type": table_row[4],
                    "description": table_row[3] if table_row[3] else None
                }
                
                # Get columns
                cursor.execute(COLUMNS_QUERY, (table_name, schema, schema))
                columns = []
                for row in cursor.fetchall():
                    columns.append({
                        "name": row[0],
                        "type": row[1],
                        "length": row[2],
                        "precision": row[3],
                        "nullable": bool(row[4]),
                        "description": row[5] if row[5] else None
                    })
                result["columns"] = columns
                
                # Get indexes
                cursor.execute(INDEXES_QUERY, (table_name, schema, schema))
                indexes = []
                for row in cursor.fetchall():
                    indexes.append({
                        "name": row[0],
                        "type": row[1],
                        "description": row[2] if row[2] else None,
                        "keys": row[3]
                    })
                result["indexes"] = indexes
                
                # Get constraints
                cursor.execute(CONSTRAINTS_QUERY, (table_name, schema, schema))
                constraints = []
                for row in cursor.fetchall():
                    constraints.append({
                        "name": row[0],
                        "type": row[1],
                        "keys": row[2]
                    })
                result["constraints"] = constraints
                
                # Get foreign keys
                cursor.execute(FOREIGN_KEY_QUERY, (schema, schema, table_name))
                foreign_keys = []
                for row in cursor.fetchall():
                    foreign_keys.append({
                        "name": row[0],
                        "schema": row[1],
                        "table_name": row[2],
                        "column_name": row[3],
                        "referenced_schema": row[4],
                        "referenced_table": row[5],
                        "referenced_column": row[6]
                    })
                result["foreignKeys"] = foreign_keys
                
                return DbOperationResult(success=True, data=result)
                
            finally:
                connection.close()
                
        except Exception as ex:
            self._logger.error(f"DescribeTable failed: {str(ex)}")
            return DbOperationResult(success=False, error=str(ex))
