#!/usr/bin/env python3
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
Entry point for the MSSQL MCP Server.
"""

import asyncio
import sys
import os

# Add the src directory to the Python path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'src'))

from src.mssql_mcp.server import main

if __name__ == "__main__":
    asyncio.run(main())
