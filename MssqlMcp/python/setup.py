# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

"""
Setup script for MSSQL MCP Server Python implementation.
"""

from setuptools import setup, find_packages
import os

# Read the README file
def read_readme():
    readme_path = os.path.join(os.path.dirname(__file__), 'README.md')
    if os.path.exists(readme_path):
        with open(readme_path, 'r', encoding='utf-8') as f:
            return f.read()
    return ""

# Read requirements
def read_requirements():
    requirements_path = os.path.join(os.path.dirname(__file__), 'requirements.txt')
    if os.path.exists(requirements_path):
        with open(requirements_path, 'r', encoding='utf-8') as f:
            return [line.strip() for line in f if line.strip() and not line.startswith('#')]
    return []

setup(
    name="mssql-mcp-server",
    version="1.0.0",
    description="A Python implementation of the Model Context Protocol (MCP) server for Microsoft SQL Server database operations",
    long_description=read_readme(),
    long_description_content_type="text/markdown",
    author="Microsoft Corporation",
    author_email="",
    url="https://github.com/microsoft/SQL-AI-samples",
    packages=find_packages(where="src"),
    package_dir={"": "src"},
    classifiers=[
        "Development Status :: 4 - Beta",
        "Intended Audience :: Developers",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.8",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
        "Topic :: Database",
        "Topic :: Software Development :: Libraries :: Python Modules",
    ],
    python_requires=">=3.8",
    install_requires=read_requirements(),
    entry_points={
        "console_scripts": [
            "mssql-mcp-server=mssql_mcp.server:main",
        ],
    },
    keywords="mcp model-context-protocol sql-server database mssql",
    project_urls={
        "Bug Reports": "https://github.com/microsoft/SQL-AI-samples/issues",
        "Source": "https://github.com/microsoft/SQL-AI-samples",
        "Documentation": "https://github.com/microsoft/SQL-AI-samples/tree/main/MssqlMcp/python",
    },
)
