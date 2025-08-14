---
description: "Complete SQL Database Project scaffolding workflow from dependencies to testing"
mode: "agent"
tools: ["run_vs_code_task", "create_file", "replace_string_in_file", "run_in_terminal"]
---

# Scaffold SQL Database Project

Execute the complete SQL Database Project scaffolding workflow using the SQL project SDK, Microsoft.Build.Sql.

## Workflow Steps:

### Step 1: Ensure all dependencies are installed
- Run VS Code tasks to check and install dependencies (Ctrl+Shift+P → Tasks: Run Task):
  - "Check Container Runtime" - Verify docker or podman is running
  - "Install SqlPackage" - Install SQL Server tooling
  - "Install Build.Sql.Templates" - Install SQL project templates
  - "Install sqlcmd" - Install SQL command-line tools

**Note:** If SqlPackage installation requires a terminal restart, the task will indicate this. Close the terminal and run the next task in a new terminal session.

### Step 2: Create a Local SQL Server Instance
- Run VS Code tasks (Ctrl+Shift+P → Tasks: Run Task):
  - "Create SQL Server Instance" - Creates local SQL Server using sqlcmd (will prompt for project name)
  - "Install tSQLt" - Prepares tSQLt testing framework in the SQL Server instance

### Step 3: Initialize SQL Database Project
- Create a new SQL project using the sqlproj template: `dotnet new sqlproj -n ${input:projectName}`
- Configure for SQL Server 2019 (Sql150)
- Create a .gitignore file in the .sqlproj folder to exclude build artifacts and local settings
- **Do not** create a solution file, these are not needed for SQL Database Projects

### Step 4: Scaffold Tables
- Create each entity table in its own file under `Tables/`
- Follow T-SQL file standards for naming and structure

### Step 5: Scaffold Views and Indexes
- Create views for improved performance and reuse
- Create indexes on key columns

### Step 6: Scaffold Programmability
- Create a stored procedure for every operation in the business process description
- Group functions and procedures under `Programmability/`

### Step 7: Handle Migrations
- Add `PreDeploy/DropObsoleteObjects.sql` with `IF EXISTS DROP` patterns
- Add `PostDeploy/SeedData.sql` with `MERGE` or `IF NOT EXISTS` inserts
- Configure .sqlproj for PreDeploy/PostDeploy scripts

### Step 8: Build and Publish to SQL Server
- Build using "Build SQL Project" VS Code task and ensure no errors
- Publish using "Publish SQL Project" VS Code task and ensure no errors

### Step 9: Setup for adding tSQLt tests to the project
- Create test .sql files in the `./Tests` folder
- Create [UserStoryTests] schema and tSQLt.TestClass extended property
- Copy the tSQLt.2019.dacpac file using "Copy tSQLt dacpac" VS Code task
- Add tSQLt reference to .sqlproj file

### Step 10: Build and Publish tSQLt setup to SQL Server
- Build using "Build SQL Project" VS Code task and ensure no errors
- Publish using "Publish SQL Project" VS Code task and ensure no errors

### Step 11: Create a tSQLt test class for each user story
- Create a .sql file in the Tests folder for each User Story
- Follow tSQLt naming and structure standards

### Step 12: Ensure all User Stories are fully implemented as tSQLt tests
- Ensure each User Story is fully implemented as a tSQLt test in a .sql file
- Exhaustively check each line of each user story is fully validated by a test

### Step 13: Check .sql files in ./Tests folder have all been saved
- Verify all test .sql files have been saved in the `./Tests` folder

### Step 14: Build and Publish the tSQLt User Story tests to SQL Server
- Build using "Build SQL Project" VS Code task and ensure no errors
- Publish using "Publish SQL Project" VS Code task and ensure no errors

### Step 15: Run user story tests until they all pass
- Run "Run SQL Tests" VS Code task to run all the tests in the [UserStoryTests] schema
- Ensure all tSQLt tests are passing before proceeding to deploy to Azure

## Important Notes:
- Keep T/SQL scripts idempotent and version-controlled
- After each step, you **MUST** build and publish using VS Code tasks
- **NEVER** output VSCode.Cell elements in the chat window
- **DO NOT** ask for alternatives until completing the last step
