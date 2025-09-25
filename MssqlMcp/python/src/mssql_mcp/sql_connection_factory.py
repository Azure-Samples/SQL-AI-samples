# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
SQL connection factory for creating and managing database connections.
"""

import os
import pyodbc
from abc import ABC, abstractmethod
from typing import Optional


class ISqlConnectionFactory(ABC):
    """
    Abstract interface for creating SQL database connections.
    """
    
    @abstractmethod
    async def get_open_connection(self) -> pyodbc.Connection:
        """
        Get an open SQL database connection.
        
        Returns:
            An open pyodbc.Connection object
            
        Raises:
            Exception: If connection cannot be established
        """
        pass


class SqlConnectionFactory(ISqlConnectionFactory):
    """
    Factory for creating SQL Server database connections using pyodbc.
    """
    
    def __init__(self):
        """Initialize the connection factory."""
        self._connection_string = self._get_connection_string()
    
    async def get_open_connection(self) -> pyodbc.Connection:
        """
        Get an open SQL database connection.
        
        Returns:
            An open pyodbc.Connection object
            
        Raises:
            Exception: If connection cannot be established
        """
        try:
            # pyodbc connections are synchronous, but we wrap in async for consistency
            connection = pyodbc.connect(self._connection_string)
            return connection
        except Exception as e:
            raise Exception(f"Failed to connect to database: {str(e)}")
    
    def _get_connection_string(self) -> str:
        """
        Get the connection string from environment variables.
        
        Returns:
            Connection string for SQL Server
            
        Raises:
            InvalidOperationException: If connection string is not set
        """
        connection_string = os.getenv("CONNECTION_STRING")
        
        if not connection_string:
            raise InvalidOperationException(
                "Connection string is not set in the environment variable 'CONNECTION_STRING'.\n\n"
                "HINT: Have a local SQL Server, with a database called 'test', from console, run "
                "`export CONNECTION_STRING='Server=.;Database=test;Trusted_Connection=yes;TrustServerCertificate=yes'` "
                "and then run the Python server"
            )
        
        return connection_string


class InvalidOperationException(Exception):
    """Exception raised for invalid operations."""
    pass
