
# Guidelines for AI Assistants: Scaffolding SQL Database Projects for T/SQL Applications

This notebook guides AI assistants through a step-by-step chain-of-thought to generate a full SQL Database Application using the SQL project SDK, Microsoft.Build.Sql

Each database object **must** be placed in its own `.sql` file, organized into a coherent folder structure. (this makes it easier for VSCode agent-mode to work with files)

The application should expose Stored Procedures and Views for all operations (all tables should be hidden as internal to the application).

NOTES:
- You (the AI Agent) **must not** edit this file.

## Summary

- Use SQL Database Projects (.sqlproj) to manage schema as code. (Target SQL Server 2019, as this is the latest version that tSQLt supports via a .dacpac)
 - You can find the full documentation for `Sql Database Projects` here: 
  - https://raw.githubusercontent.com/MicrosoftDocs/sql-docs/refs/heads/live/docs/tools/sql-database-projects/sql-database-projects.md
 - Keep T/SQL scripts idempotent and version-controlled.
- For local deployment, use `sqlcmd create mssql` to create a local SQL Server instance. 
 - use the helper script `create-mssql-instance-using-sqlcmd.ps1` in the `./.github/tsql/install` folder
  - To get the connection string, run `sqlcmd config connection-strings` (use the helper script `get-sql-connection-string.ps1` in the `./.github/tsql/install` folder).

## Considerations

- Folder structure maps directly to SQL object types.
- Idempotency: scripts must handle repeated executions.
- Naming conventions align with best practices.
- After each step, you **MUST**:
  - build (using `Build.ps1`, which takes `-ProjectName` as the single parameter)
    - `Build.ps1` is in the `./.github/tsql/inner-loop` folder.
  - publish (using `Publish.ps1`, which takes `-ProjectName` as the single parameter)
    - `Publish.ps1` is in the `./.github/tsql/inner-loop` folder. 
    - `Publish.ps1` uses `SqlPackage` to publish the project to the SQL Server.

NOTES:
- **NEVER** output VSCode.Cell elements in the chat window.  This will cause the chat to break, instead **Always** put output into files in the workspace (as instructed).
- **DO NOT** ask me if I want to do something OR something else.  Until you've completed the last step in this chain of thought, you must not ask me if I want to do something else.  Just do each step as instructed.

## Project Structure

---
```text
MyDatabaseProject/               -- root of .sqlproj
  MyDatabaseProject.sqlproj      -- SQL Database Project file
  Properties/                    -- Database settings and publish profiles
  Tables/                        -- CREATE TABLE scripts
  Views/                         -- CREATE VIEW scripts
  Programmability/               -- Functions, Stored Procedures
    Functions/
    Stored Procedures/
  PreDeploy/                     -- Pre-deployment scripts
  PostDeploy/                    -- Post-deployment scripts and data migrations
  Tests/                         -- tSQLt or other test scripts
```
---

NOTES: 
1. **DO NOT*** put any spaces in the folder names, or in the file names.  Use PascalCase for folder names and file names.
2. **DO NOT** create `.keep` files, they are not needed.
3. You **MUST NOT** put square brackets, `[` and `]`, in file names or folder names.
4. You **MUST NOT** ever add GO statements to any SQL scripts you generate.
5. All Primary Key ID columns must use IDENTITY(1,1)
6. Table IDs must never be used as Stored Procedures parameters
7. **NEVER** use table valued parameters (TVPs) as Stored Procedure parameters (that will be used as MCP Tools), this is because Data Api Builder does not support table valued parameters in entities.

### Step 0: Thoroughly review the business-process-description.md file

**Thought:** Understand the business process and the entities involved, and look for inconsistencies between the Roles, Entities, Entity Descriptions, Operations and User Stories.
- Look for any missing entities, entity descriptions, or missing operations in the business-process-description.md file.
- Look for any missing User Stories in the business-process-description.md file.

**Action:**
- In the agent-mode chat window, strongly suggest any missing entities, entity descriptions, or operations or user stories to the user.  Give very clear reasons why you feel something is missing, especially if you think there is an inconsistency between the Roles, Entities, Entity Descriptions, Operations and User Stories.
- Do not edit the business-process-description.md file directly, just suggest changes in the chat window.
- Particularly look for missing user stories, especially negative cases, i.e. user stories that test unintended behavior by the roles.

### Step 1: Ensure all dependencies are installed

**Thought:** Ensure all dependencies are installed for the SQL Database Project.  Make sure you run these actions one at a time (don't try to concatenate them with ; or &).

**Action:**
- Ensure a container runtime (e.g. docker or podman) is running by running `./.github/tsql/install/is-container-runtime-running.ps1`
  - If a container runtime is not running, ensure the user does not proceed until a container runtime is installed and running.
- Ensure `SqlPackage` is installed by running `./.github/tsql/install/ensure-sqlpackage-installed.ps1`
  - NOTE: If the following message is returned after installing `SqlPackage`: `Since you just installed the .NET SDK, you will need
  to reopen the Command Prompt window before running the tool you installed.`, run `exit` in the
  terminal window, so another terminal window will be created for the next command.
    - NOTE: Running `exit` in the chat window will cause the Agent to hang.  The user will need to clik the `Stop` button in the
    chat window to stop the agent and type `proceed` to continue.
- Ensure `Build.Sql.Templates` is installed by running `./.github/tsql/install/ensure-build-sql-templates-installed.ps1`  
- Ensure `sqlcmd` is installed by running `./.github/tsql/install/ensure-sqlcmd-installed.ps1`

### Step 2: Create a Local SQL Server Instance

**Thought:** Use sqlcmd to create a local SQL Server instance for development.

**Action:**
- Create a local SQL Server instance using the script `create-mssql-instance-using-sqlcmd.ps1` in the `./.github/tsql/install` folder.  Pass in the name of the project name as the `-ProjectName` parameter. (this will become the `sqlcmd` `context` name)
- Ensure `tSQLt` is 'prepared' in SQL Server instance by running `./.github/tsql/install/ensure-tsqlt-installed.ps1`

### Step 3: Initialize SQL Database Project

**Thought:** Create a new project to track schema in source control.

**Action:**
- Create a new SQL project using the sqlproj template e.g.: `dotnet new sqlproj -n MyDatabaseProject`
- Make sure the .sqlproj is set to work with SQL Server 2019 (which is Sql150), i.e.: make sure this is in the .sqlproj file:
  - `<DSP>Microsoft.Data.Tools.Schema.Sql.Sql150DatabaseSchemaProvider</DSP>`
  - This is because the .dacpac for tSQLt is not available for SQL Server 2022 (Sql160) yet.
- Create a .gitignore file in the .sqlproj folder to exclude build artifacts and local settings:

NOTE: **Do not** create a solution file, these are not needed for SQL Database Projects.

### Step 4: Scaffold Tables

**Thought:** Each entity table resides in its own file under `Tables/`.  

NOTE:
- Do not include individual files you create in the .sqlproj.
  - This is becasue the .NET SDK includes 'Build' items from your project directory by default. (otherwise you will get `error NETSDK1022` when you try to build the project due to Duplicate items)

**Action:**
- e.g. Create `Tables/Customer.sql`:

---
```sql
  CREATE TABLE dbo.Customer (
    CustomerId INT NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL DEFAULT(1)
  );
```
---

- Repeat for each business entity.

### Step 5: Scaffold Views and Indexes

**Thought:** Views and indexes improve performance and reuse.

**Action:**
- `Views/ActiveCustomers.sql`: `CREATE VIEW dbo.ActiveCustomers AS SELECT * FROM dbo.Customer WHERE IsActive = 1;`
- `Tables/Customer.indexes.sql`: `CREATE INDEX IX_Customer_Name ON dbo.Customer(Name);`

### Step 6: Scaffold Programmability

**Thought:** Group functions and procedures under `Programmability/`.

**Action:**
- Create a stored procedure for every operation in the business process description.
- Internal IDENTITY value must not be exposed as a parameter.
  - Since operations will only be as tools, all parameters will be `NVARCHAR(MAX)`, and all parameters must take text values, and IDENTITY values must be looked up internally to the stored procedure.
- Use `SET NOCOUNT ON`, parameter validation, `TRY...CATCH`, transactions.

### Step 7: Handle Migrations

**Thought:** Use PreDeploy/PostDeploy for data migrations and seed data.

**Action:**
- Add `PreDeploy/DropObsoleteObjects.sql` with `IF EXISTS DROP` patterns.
- Add `PostDeploy/SeedData.sql` with `MERGE` or `IF NOT EXISTS` inserts.  

NOTE: You must follow the documentation here to configure the .sqlproj correctly: https://raw.githubusercontent.com/MicrosoftDocs/sql-docs/refs/heads/live/docs/tools/sql-database-projects/concepts/pre-post-deployment-scripts.md, i.e. ensure a PreDeploy and PostDeploy element is included in the .sqlproj

NOTE: You **must** set the BuildAction to None in the .sqlproj for the PreDeploy and PostDeploy files (otherwise error SQL71006 will happen at deployment time).  See this StackOverFlow post for details:
  - https://stackoverflow.com/questions/18698481/error-in-sql-script-only-one-statement-is-allowed-per-batch

So the .sqlproj should have two additional ItemGroups that look like this:

---
```
  <ItemGroup>
    <PreDeploy Include="PreDeploy\DropObsoleteObjects.sql" />
    <PostDeploy Include="PostDeploy\SeedData.sql" / >
  </ItemGroup>
```
---

and

---
```
  <ItemGroup>
    <Build Remove="PreDeploy\DropObsoleteObjects.sql" />
    <Build Remove="PostDeploy\SeedData.sql" />
  </ItemGroup>
```
---

### Step 8: Build and Publish to SQL Server

**Thought:** Run a build of the .sqlproj, and then publish the project to the SQL Server using SqlPackage.

**Action:**
- Build by running `Build.ps1` (which takes `-ProjectName` as the single parameter) and ensure no errors.
- Publish to the SQL Server using SqlPackage by running `Publish.ps1` (which takes `-ProjectName` as the single parameter) and ensure no errors.

### Step 9: Setup for adding tSQLt tests to the project

**Action:**
- Create test .sql files in the `./Tests` folder.
- Create a .sql file that creates the [UserStoryTests] schema with dbo authorization. 
- Add another .sql file that creates the 'tSQLt.TestClass' extended property on the 'UserStoryTests' SCHEMA object.  This enables tSQLt for the [UserStoryTests] schema i.e.:
  `EXECUTE sp_addextendedproperty @name = N'tSQLt.TestClass', @value = 1, @level0type = N'SCHEMA', @level0name = N'UserStoryTests';`
- Copy the tSQLt.2019.dacpac file to the `./Tests` folder using the `copy-tSQLt.dacpac-file.ps1` script. (the script takes -ProjectPath parameter)
- Add a reference in the .sqlproj file to the tSQLt.2019.dacpac file in the `./Tests` folder.  This is done by adding a new ItemGroup to the .sqlproj file that looks like this:

---
```
  <ItemGroup>
    <ArtifactReference Include="Tests\tSQLt.2019.dacpac">
      <SuppressMissingDependenciesErrors>False</SuppressMissingDependenciesErrors>
    </ArtifactReference>
  </ItemGroup>
```
---

### Step 10: Build and Publish tSQLt setup to SQL Server

**Action:**
- Build by running `Build.ps1` (which takes `-ProjectName` as the single parameter) and ensure no errors.
- Publish to the SQL Server using SqlPackage by running `Publish.ps1` (which takes `-ProjectName` as the single parameter) and ensure no errors.

### Step 11: Create a tSQLt test class for each user story

**Thought:** For each User Story in the Business Process Description create a .sql file with a single stored procedure that tests the user story.  
- The test class should be named after the user story and **MUST** be in the [UserStoryTests] schema.  
- All stored procedures names **MUST** start with the word `test`.  
  - tSQLt requires all stored procedures to start with the word `test`.
- All tSQLt parameter names are Pascal Case, and **MUST** start with an UPPER case letter.
  - i.e. to avoid this build warning `Build warning SQL71558: The object reference [tSQLt].[AssertEquals].[@expected] differs only by case from the object definition [tSQLt].[AssertEquals].[@Expected]. `.  Make sure you use Pascal Case parameter names for tSQLt programmability objects like `@Expected` (and **NOT** `@expected`).

**Action:**
- Create a .sql file in the Tests folder for each User Story.
- Make sure each user story is in one .sql file. 
  - You **MUST NOT** use GO separators in the test .sql files.
  - You **MUST** only put one CREATE STORED PROCEDURE statement in each test .sql file.
  - You **MUST** put the stored procedure in the [UserStoryTests] schema.
  - You **MUST** start all stored procedure names with the word `test`.
  - You **MUST NOT** put a CREATE SCHEMA statement in the test .sql files. (this is because tSQLt already created the [UserStoryTests] schema for you)

### Step 12: User to check .sql files in ./Tests folder have all been saved

**Thought:** The above step can take a long time to complete, and Agent-Mode runs
 in parallel and asynchronously, so user needs to check that all the .sql files 
 in the ./Tests folder have been saved before proceeding to the next step.

**Action:**
- The user must check that all the test .sql files have been saved in the `./Tests` folder.
- If not, user must wait to proceed until all .sql files in the ./Tests folder have been saved by the agent.

### Step 13: Build and Publish the tSQLt User Story tests to SQL Server

**Action:**
- Build by running `Build.ps1` (which takes `-ProjectName` as the single parameter) and ensure no errors.
- Publish to the SQL Server using SqlPackage by running `Publish.ps1` (which takes `-ProjectName` as the single parameter) and ensure no errors.

### Step 14: Run user story tests until they all pass.

**Action:**
- Run `Test.ps1` (which takes `-ProjectName` as the single parameter) to run all the tests in the [UserStoryTests] schema. `Test.ps1` is in the `./.github/tsql/inner-loop` folder.
- Ensure all tSQLt tests are passing before proceeding to deploy to Azure.

### THE END

### Conclusion

By following this chain-of-thought guide, AI assistants can scaffold a robust SQL Database Project with clear separation of concerns, versioning and a well defined Build, Publish, Test inner-loop for local development.

