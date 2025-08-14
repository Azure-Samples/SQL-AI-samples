---
description: "SQL Database Project structure and configuration standards"
applyTo: "**/*.sqlproj"
---

# SQL Database Project Structure

## Project Structure

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

## Project Configuration

- Each database object **must** be placed in its own `.sql` file, organized into a coherent folder structure.
- Folder structure maps directly to SQL object types.
- Do not include individual files you create in the .sqlproj. This is because the .NET SDK includes 'Build' items from your project directory by default. (otherwise you will get `error NETSDK1022` when you try to build the project due to Duplicate items)

## SQL Server Version Configuration

- Make sure the .sqlproj is set to work with SQL Server 2019 (which is Sql150):
  ```xml
  <DSP>Microsoft.Data.Tools.Schema.Sql.Sql150DatabaseSchemaProvider</DSP>
  ```
- This is because the .dacpac for tSQLt is not available for SQL Server 2022 (Sql160) yet.

## PreDeploy/PostDeploy Configuration

You **must** set the BuildAction to None in the .sqlproj for the PreDeploy and PostDeploy files (otherwise error SQL71006 will happen at deployment time).

The .sqlproj should have two additional ItemGroups:

```xml
<ItemGroup>
  <PreDeploy Include="PreDeploy\DropObsoleteObjects.sql" />
  <PostDeploy Include="PostDeploy\SeedData.sql" />
</ItemGroup>
```

and

```xml
<ItemGroup>
  <Build Remove="PreDeploy\DropObsoleteObjects.sql" />
  <Build Remove="PostDeploy\SeedData.sql" />
</ItemGroup>
```

## tSQLt Integration

Add a reference in the .sqlproj file to the tSQLt.2019.dacpac file in the `./Tests` folder:

```xml
<ItemGroup>
  <ArtifactReference Include="Tests\tSQLt.2019.dacpac">
    <SuppressMissingDependenciesErrors>False</SuppressMissingDependenciesErrors>
  </ArtifactReference>
</ItemGroup>
```
