
---
description: "T-SQL best practices and coding standards for production-grade stored procedures"
applyTo: "**/*.sql"
---

# T-SQL Best Practices Guide

This guide outlines good practices and standards for writing high-quality, maintainable, and performant T-SQL code, with a focus on production-grade stored procedures.

## Table of Contents

1. Introduction
2. Naming Conventions
3. Formatting and Style
4. Schema and Object Management
5. Stored Procedure Guidelines
6. Error Handling and Logging
7. Transaction Management
8. Performance Optimization
9. Security and Permissions
10. Maintainability and Documentation
11. Common Anti-Patterns
12. Tools and Automation

---

## 1. Introduction

Writing effective T/SQL requires consistency, clarity, and attention to performance. This guide consolidates best practices around naming, formatting, error handling, and anti-pattern avoidance to help teams deliver robust database code.

## 2. Naming Conventions

- Use `PascalCase` for objects: tables, views, procedures, functions.
- Prefix stored procedures with the schema name, e.g., `dbo.GetCustomerOrders`.
- Avoid using `sp_` prefix (reserved for system procedures).
- Use meaningful names: include entity and action, e.g., `Sales.InsertOrder`.
- Prefix parameters with `@` and use camelCase: `@customerId`.
- Use singular nouns for tables: `Customer` instead of `Customers`.

## 3. Formatting and Style

- Use `SET NOCOUNT ON` at the top of stored procedures to suppress extra row count messages.
- Align keywords on separate lines:
  ```sql
  SELECT
      c.CustomerId,
      c.Name
  FROM
      dbo.Customer AS c
  WHERE
      c.IsActive = 1;
  ```
- Indent by four spaces per level.
- Uppercase reserved words (`SELECT`, `FROM`, `WHERE`).
- Lowercase object names unless quoting is required.
- Limit line length to 120 characters.
- Group related clauses: `SELECT`, `FROM`, `JOIN`, `WHERE`, `GROUP BY`, `ORDER BY`.

## 4. Schema and Object Management

- Always schema-qualify object names: `dbo.TableName`.
- Use `IF OBJECT_ID('dbo.MyProc') IS NOT NULL DROP PROCEDURE dbo.MyProc;` for safe deployments.
- Avoid `GO` inside procedures; use batch separators only in deployment scripts.
- Version control all scripts.

## 5. Stored Procedure Guidelines

- One procedure, one responsibility.
- Parameter validation at the top:
  ```sql
  IF @param IS NULL
  BEGIN
      THROW 50000, 'Parameter @param must not be NULL.', 1;
  END;
  ```
- Use explicit column lists in `INSERT`/`UPDATE`.
- Avoid mixing DML and SELECT logic; consider splitting into helper procedures.
- Leverage `OUTPUT` clause to return identity or modified rows.
- Return consistent result sets or return codes; avoid ad-hoc print statements.

## 6. Error Handling and Logging

- Use `TRY...CATCH` blocks:
  ```sql
  BEGIN TRY
      -- operation
  END TRY
  BEGIN CATCH
      DECLARE @ErrorNumber INT = ERROR_NUMBER(),
              @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
      RAISERROR (@ErrorMessage, 16, 1);
      RETURN @ErrorNumber;
  END CATCH;
  ```
- Log errors to an audit table with context: procedure name, parameters, timestamp.
- Use proper severity levels (>= 16 for user errors).

## 7. Transaction Management

- Keep transactions as short as possible.
- Use explicit transactions:
  ```sql
  BEGIN TRANSACTION;
  -- operations
  IF @@ERROR <> 0
  BEGIN
      ROLLBACK TRANSACTION;
      RETURN;
  END;
  COMMIT TRANSACTION;
  ```
- Avoid implicit transactions; set `XACT_ABORT ON` when appropriate.

## 8. Performance Optimization

- Avoid `SELECT *`; specify columns.
- Ensure SARGability: no functions on columns in `WHERE` clauses.
- Use appropriate indexes; include only needed columns.
- Be cautious with parameter sniffing; use `OPTION (RECOMPILE)` if needed.
- Prefer set-based operations over row-by-row processing.
- Avoid cursors and scalar UDFs; consider inline table-valued functions.
- Use `TRY_CONVERT` or `TRY_CAST` for safe conversions.
- Consider `READONLY` table-valued parameters for bulk data input.

## 9. Security and Permissions

- Use least-privilege: grant execute on procedures rather than direct table access.
- Avoid dynamic SQL when possible; if required, parameterize safely with `sp_executesql`.
- Sanitize inputs to prevent SQL injection.
- Use `WITH EXECUTE AS OWNER` where cross-schema access is required.

## 10. Maintainability and Documentation

- Comment intent, not mechanics:
  ```sql
  -- Calculate total order amount per customer
  SELECT ...
  ```
- Include header comments with purpose, author, date, and change history.
- Modularize logic into views or functions for reuse.
- Keep deployment scripts idempotent.

## 11. Common Anti-Patterns

- Avoid:
  - Implicit conversions in predicates.
  - Using `sp_executesql` without parameters (dynamic SQL injection).
  - Functions in `WHERE` clauses that disable index usage.
  - Unbounded `WHILE` loops processing row-by-row.
  - Nested transactions without proper error checks.
  - Magic numbers and hard-coded literals.

---

Adhering to these guidelines will lead to consistent, secure, and performant T/SQL code suitable for production environments.
