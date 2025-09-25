#!/usr/bin/env python3
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
Example script demonstrating how to use the MSSQL MCP Server tools directly.
This is for testing and development purposes.
"""

import asyncio
import os
import sys
import logging

# Add the src directory to the Python path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'src'))

from src.mssql_mcp.sql_connection_factory import SqlConnectionFactory
from src.mssql_mcp.tools import (
    ListTablesTool,
    DescribeTableTool,
    CreateTableTool,
    ReadDataTool,
    InsertDataTool,
    UpdateDataTool,
    DropTableTool
)


async def main():
    """Example usage of the MSSQL MCP tools."""
    
    # Set up logging
    logging.basicConfig(level=logging.INFO)
    logger = logging.getLogger(__name__)
    
    # Check if connection string is set
    if not os.getenv("CONNECTION_STRING"):
        print("Please set the CONNECTION_STRING environment variable")
        print("Example: export CONNECTION_STRING='Server=.;Database=test;Trusted_Connection=yes;TrustServerCertificate=yes'")
        return
    
    try:
        # Initialize connection factory and tools
        connection_factory = SqlConnectionFactory()
        
        # Initialize tools
        list_tables = ListTablesTool(connection_factory, logger)
        describe_table = DescribeTableTool(connection_factory, logger)
        create_table = CreateTableTool(connection_factory, logger)
        read_data = ReadDataTool(connection_factory, logger)
        insert_data = InsertDataTool(connection_factory, logger)
        update_data = UpdateDataTool(connection_factory, logger)
        drop_table = DropTableTool(connection_factory, logger)
        
        print("=== MSSQL MCP Server Tools Example ===\n")
        
        # Example 1: List all tables
        print("1. Listing all tables:")
        result = await list_tables.execute()
        if result.success:
            print(f"   Found {len(result.data)} tables:")
            for table in result.data:
                print(f"   - {table}")
        else:
            print(f"   Error: {result.error}")
        print()
        
        # Example 2: Create a test table
        print("2. Creating a test table:")
        create_sql = """
        CREATE TABLE dbo.TestUsers (
            Id INT PRIMARY KEY IDENTITY(1,1),
            Name NVARCHAR(100) NOT NULL,
            Email NVARCHAR(255) UNIQUE,
            CreatedDate DATETIME2 DEFAULT GETDATE()
        )
        """
        result = await create_table.execute(create_sql)
        if result.success:
            print("   Test table created successfully")
        else:
            print(f"   Error: {result.error}")
        print()
        
        # Example 3: Describe the test table
        print("3. Describing the test table:")
        result = await describe_table.execute("dbo.TestUsers")
        if result.success:
            table_info = result.data
            print(f"   Table: {table_info['table']['name']}")
            print(f"   Schema: {table_info['table']['schema']}")
            print(f"   Columns:")
            for col in table_info['columns']:
                nullable = "NULL" if col['nullable'] else "NOT NULL"
                print(f"     - {col['name']}: {col['type']} {nullable}")
        else:
            print(f"   Error: {result.error}")
        print()
        
        # Example 4: Insert some test data
        print("4. Inserting test data:")
        insert_sql = """
        INSERT INTO dbo.TestUsers (Name, Email) VALUES 
        ('John Doe', 'john.doe@example.com'),
        ('Jane Smith', 'jane.smith@example.com'),
        ('Bob Johnson', 'bob.johnson@example.com')
        """
        result = await insert_data.execute(insert_sql)
        if result.success:
            print(f"   Inserted {result.rows_affected} rows")
        else:
            print(f"   Error: {result.error}")
        print()
        
        # Example 5: Read the data
        print("5. Reading test data:")
        read_sql = "SELECT * FROM dbo.TestUsers ORDER BY Id"
        result = await read_data.execute(read_sql)
        if result.success:
            print(f"   Found {len(result.data)} rows:")
            for row in result.data:
                print(f"     ID: {row['Id']}, Name: {row['Name']}, Email: {row['Email']}")
        else:
            print(f"   Error: {result.error}")
        print()
        
        # Example 6: Update some data
        print("6. Updating test data:")
        update_sql = "UPDATE dbo.TestUsers SET Name = 'John Updated' WHERE Id = 1"
        result = await update_data.execute(update_sql)
        if result.success:
            print(f"   Updated {result.rows_affected} rows")
        else:
            print(f"   Error: {result.error}")
        print()
        
        # Example 7: Read updated data
        print("7. Reading updated data:")
        read_sql = "SELECT * FROM dbo.TestUsers WHERE Id = 1"
        result = await read_data.execute(read_sql)
        if result.success:
            if result.data:
                row = result.data[0]
                print(f"   Updated row: ID: {row['Id']}, Name: {row['Name']}, Email: {row['Email']}")
            else:
                print("   No data found")
        else:
            print(f"   Error: {result.error}")
        print()
        
        # Example 8: Clean up - drop the test table
        print("8. Cleaning up - dropping test table:")
        drop_sql = "DROP TABLE dbo.TestUsers"
        result = await drop_table.execute(drop_sql)
        if result.success:
            print("   Test table dropped successfully")
        else:
            print(f"   Error: {result.error}")
        print()
        
        print("=== Example completed successfully ===")
        
    except Exception as ex:
        print(f"Example failed with error: {str(ex)}")
        logger.exception("Example execution failed")


if __name__ == "__main__":
    asyncio.run(main())
