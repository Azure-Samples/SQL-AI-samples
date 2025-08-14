
# Guidelines for AI Assistants: SQL Database Projects for T/SQL Applications

**PROHIBITED**: This document must NOT include terminology from meta-instruction or meta-meta-instruction management concepts.

## Overview

This document provides universal guidance for AI assistants working with SQL Database Projects using the SQL project SDK, Microsoft.Build.Sql.

The application should expose Stored Procedures and Views for all operations (all tables should be hidden as internal to the application).

NOTES:
- You (the AI Agent) **must not** edit this file.
- Use specialized instruction files (`.instructions.md`) for context-specific guidance
- Use prompt files (`.prompt.md`) for step-by-step procedures

## Universal Principles

- Use SQL Database Projects (.sqlproj) to manage schema as code
- Target SQL Server 2019 (latest version that tSQLt supports via a .dacpac)
- Keep T/SQL scripts idempotent and version-controlled
- Each database object **must** be placed in its own `.sql` file
- Organize files into a coherent folder structure for easier VS Code agent-mode work

## Development Workflow

- For local deployment, use `sqlcmd create mssql` to create a local SQL Server instance
- Use VS Code tasks for all build, publish, and test operations
- Use the "Create SQL Server Instance" VS Code task for setup
- Use the "Get SQL Connection String" VS Code task for connection information

## Quality Standards

- Folder structure maps directly to SQL object types
- Idempotency: scripts must handle repeated executions
- Naming conventions align with best practices
- After each development step, you **MUST**:
  - build (using "Build SQL Project" VS Code task)
  - publish (using "Publish SQL Project" VS Code task)

## Content Organization

- **Specialized Guidance**: See `.github/instructions/` for context-specific standards
- **Step-by-Step Procedures**: See `.github/prompts/` for reusable workflows
- **Task Automation**: See `.vscode/tasks.json` for parameterized operations

## Documentation References

- SQL Database Projects documentation: https://raw.githubusercontent.com/MicrosoftDocs/sql-docs/refs/heads/live/docs/tools/sql-database-projects/sql-database-projects.md
- Pre/Post deployment scripts: https://raw.githubusercontent.com/MicrosoftDocs/sql-docs/refs/heads/live/docs/tools/sql-database-projects/concepts/pre-post-deployment-scripts.md

