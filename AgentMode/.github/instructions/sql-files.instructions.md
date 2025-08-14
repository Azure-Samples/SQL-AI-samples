---
description: "T-SQL coding standards and file naming conventions for SQL Database Projects"
applyTo: "**/*.sql"
---

# T-SQL File Standards

## File and Folder Naming Conventions

1. **DO NOT** put any spaces in folder names or file names. Use PascalCase for folder names and file names.
2. **DO NOT** create `.keep` files, they are not needed.
3. You **MUST NOT** put square brackets, `[` and `]`, in file names or folder names.

## T-SQL Script Standards

4. You **MUST NOT** ever add GO statements to any SQL scripts you generate.
5. All Primary Key ID columns must use IDENTITY(1,1)
6. Table IDs must never be used as Stored Procedures parameters
7. **NEVER** use table valued parameters (TVPs) as Stored Procedure parameters (that will be used as MCP Tools), this is because Data Api Builder does not support table valued parameters in entities.

## Stored Procedure Standards

- Internal IDENTITY value must not be exposed as a parameter.
- Since operations will only be as tools, all parameters will be `NVARCHAR(MAX)`, and all parameters must take text values, and IDENTITY values must be looked up internally to the stored procedure.
- Use `SET NOCOUNT ON`, parameter validation, `TRY...CATCH`, transactions.

## tSQLt Testing Standards

- All tSQLt parameter names are Pascal Case, and **MUST** start with an UPPER case letter.
- i.e. to avoid this build warning `Build warning SQL71558: The object reference [tSQLt].[AssertEquals].[@expected] differs only by case from the object definition [tSQLt].[AssertEquals].[@Expected]`. Make sure you use Pascal Case parameter names for tSQLt programmability objects like `@Expected` (and **NOT** `@expected`).
- All stored procedures names **MUST** start with the word `test` for tSQLt requirements.
- You **MUST NOT** use GO separators in test .sql files.
- You **MUST** only put one CREATE STORED PROCEDURE statement in each test .sql file.
- You **MUST** put the stored procedure in the [UserStoryTests] schema.
- You **MUST NOT** put a CREATE SCHEMA statement in the test .sql files.
