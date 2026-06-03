# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
MSSQL MCP Server implementation.
"""

import asyncio
import json
import logging
import sys
from typing import Dict, Any, List, Optional

from mcp.server import Server
from mcp.server.stdio import stdio_server
from mcp.types import (
    CallToolRequest,
    CallToolResult,
    ListToolsRequest,
    ListToolsResult,
    Tool,
    TextContent,
    ImageContent,
    EmbeddedResource
)

from .sql_connection_factory import SqlConnectionFactory
from .tools import (
    ListTablesTool,
    DescribeTableTool,
    CreateTableTool,
    DropTableTool,
    ReadDataTool,
    InsertDataTool,
    UpdateDataTool
)


class MssqlMcpServer:
    """
    MSSQL MCP Server that provides database tools via the Model Context Protocol.
    """
    
    def __init__(self):
        """Initialize the MSSQL MCP Server."""
        self.logger = self._setup_logging()
        self.connection_factory = SqlConnectionFactory()
        self.server = Server("mssql-mcp-server")
        self.tools = self._initialize_tools()
        self._setup_handlers()
    
    def _setup_logging(self) -> logging.Logger:
        """Set up logging configuration."""
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
            handlers=[
                logging.StreamHandler(sys.stderr)
            ]
        )
        return logging.getLogger(__name__)
    
    def _initialize_tools(self) -> Dict[str, Any]:
        """Initialize all available tools."""
        return {
            "list_tables": ListTablesTool(self.connection_factory, self.logger),
            "describe_table": DescribeTableTool(self.connection_factory, self.logger),
            "create_table": CreateTableTool(self.connection_factory, self.logger),
            "drop_table": DropTableTool(self.connection_factory, self.logger),
            "read_data": ReadDataTool(self.connection_factory, self.logger),
            "insert_data": InsertDataTool(self.connection_factory, self.logger),
            "update_data": UpdateDataTool(self.connection_factory, self.logger)
        }
    
    def _setup_handlers(self):
        """Set up MCP request handlers."""
        self.server.list_tools = self._handle_list_tools
        self.server.call_tool = self._handle_call_tool
    
    async def _handle_list_tools(self, request: ListToolsRequest) -> ListToolsResult:
        """
        Handle list tools request.
        
        Args:
            request: List tools request
            
        Returns:
            List of available tools
        """
        tools = []
        
        for tool_name, tool_instance in self.tools.items():
            tool = Tool(
                name=tool_instance.name,
                description=tool_instance.description,
                inputSchema={
                    "type": "object",
                    "properties": self._get_tool_properties(tool_name),
                    "required": self._get_required_properties(tool_name)
                }
            )
            tools.append(tool)
        
        return ListToolsResult(tools=tools)
    
    def _get_tool_properties(self, tool_name: str) -> Dict[str, Any]:
        """Get input schema properties for a tool."""
        if tool_name == "describe_table":
            return {
                "table_name": {
                    "type": "string",
                    "description": "Name of table"
                }
            }
        elif tool_name in ["create_table", "drop_table", "read_data", "insert_data", "update_data"]:
            return {
                "sql": {
                    "type": "string",
                    "description": "SQL statement to execute"
                }
            }
        else:
            return {}
    
    def _get_required_properties(self, tool_name: str) -> List[str]:
        """Get required properties for a tool."""
        if tool_name == "describe_table":
            return ["table_name"]
        elif tool_name in ["create_table", "drop_table", "read_data", "insert_data", "update_data"]:
            return ["sql"]
        else:
            return []
    
    async def _handle_call_tool(self, request: CallToolRequest) -> CallToolResult:
        """
        Handle call tool request.
        
        Args:
            request: Call tool request
            
        Returns:
            Tool execution result
        """
        tool_name = request.params.name
        arguments = request.params.arguments or {}
        
        try:
            if tool_name not in self.tools:
                return CallToolResult(
                    content=[TextContent(type="text", text=f"Unknown tool: {tool_name}")],
                    isError=True
                )
            
            tool_instance = self.tools[tool_name]
            result = await self._execute_tool(tool_instance, arguments)
            
            if result.success:
                content_text = json.dumps(result.to_dict(), indent=2)
                return CallToolResult(
                    content=[TextContent(type="text", text=content_text)]
                )
            else:
                return CallToolResult(
                    content=[TextContent(type="text", text=f"Error: {result.error}")],
                    isError=True
                )
                
        except Exception as ex:
            self.logger.error(f"Tool execution failed: {str(ex)}")
            return CallToolResult(
                content=[TextContent(type="text", text=f"Tool execution failed: {str(ex)}")],
                isError=True
            )
    
    async def _execute_tool(self, tool_instance: Any, arguments: Dict[str, Any]) -> Any:
        """Execute a tool with the given arguments."""
        if hasattr(tool_instance, 'execute'):
            if tool_instance.name == "describe_table":
                return await tool_instance.execute(arguments.get("table_name", ""))
            elif tool_instance.name in ["create_table", "drop_table", "read_data", "insert_data", "update_data"]:
                return await tool_instance.execute(arguments.get("sql", ""))
            else:
                return await tool_instance.execute()
        else:
            raise ValueError(f"Tool {tool_instance.name} does not have an execute method")
    
    async def run(self):
        """Run the MCP server."""
        try:
            async with stdio_server() as (read_stream, write_stream):
                await self.server.run(
                    read_stream,
                    write_stream,
                    self.server.create_initialization_options()
                )
        except Exception as ex:
            self.logger.error(f"Server error: {str(ex)}")
            raise


async def main():
    """Main entry point for the MCP server."""
    server = MssqlMcpServer()
    await server.run()


if __name__ == "__main__":
    asyncio.run(main())
