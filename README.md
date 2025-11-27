# SQL Server MCP 🛢

A **Model Context Protocol (MCP) server for Microsoft SQL Server** built for real-world enterprise database work.

I'm a data architect and engineer working in national-scale, highly regulated environments. This server powers my daily workflows—exploring unfamiliar schemas, prototyping queries, validating data pipelines in UAT, then porting that work to production with confidence. If you spend your days inside SQL Server and want AI tooling that actually understands your database, this is for you.

> Forked from Microsoft's [SQL-AI-samples](https://github.com/Azure-Samples/SQL-AI-samples) (MIT license). I've stripped out the sample notebooks to focus exclusively on a lean, production-grade MCP server.

---

## Why this exists

Most SQL + AI demos stop at "generate a query." That's table stakes. Real database work means:

- **Navigating massive schemas** you didn't design, often with cryptic naming conventions
- **Understanding data shape** before writing anything
- **Working safely** in regulated environments where one bad UPDATE can trigger an incident
- **Moving fast in UAT** so you can validate changes before they hit production

This server is built around those realities. It's stable, secure by default, and designed to make AI assistants genuinely useful on enterprise SQL Server instances.

---

## What's here today

| Capability | Details |
| --- | --- |
| **Semantic schema discovery** | `search_schema` finds tables and columns by pattern with pagination, plus fuzzy matching (Levenshtein) when you misspell things. Large schemas return deterministic slices instead of flooding the transport. |
| **Table profiling** | `profile_table` analyzes column statistics, cardinality, null percentages, and value distributions—understand data shape before writing queries. |
| **Relationship mapping** | `inspect_relationships` maps inbound/outbound foreign keys with column mappings and referential actions. Navigate unfamiliar schemas by following the FK graph. |
| **Flexible authentication** | SQL auth, Windows auth, and Azure AD—not just Azure AD callback. Configure what fits your environment. |
| **Safe data operations** | `read_data`, `insert_data`, `update_data`, `describe_table`, `list_table`, plus DDL helpers (`create_table`, `create_index`, `drop_table`) for agent-mode workflows. |
| **MCP-native** | Works with Windsurf, Claude Desktop, and any MCP-compatible client out of the box. |

### Available Tools

| Tool | Mode | Description |
|------|------|-------------|
| `list_table` | Read | Lists tables in the database, optionally filtered by schema |
| `describe_table` | Read | Describes the schema (columns and types) of a specified table |
| `read_data` | Read | Executes SELECT queries with security validation |
| `search_schema` | Read | Searches tables/columns using wildcard patterns with fuzzy fallback |
| `profile_table` | Read | Profiles a table with column statistics, cardinality, and distributions |
| `inspect_relationships` | Read | Maps inbound/outbound foreign key relationships |
| `insert_data` | Write | Inserts records into a table |
| `update_data` | Write | Updates records (WHERE clause required) |
| `create_table` | Write | Creates new tables |
| `create_index` | Write | Creates indexes on table columns |
| `drop_table` | Write | Drops tables from the database |

---

## Quick start

```bash
cd MssqlMcp/Node
npm install
npm run build
```

Configure your MCP client to point at `MssqlMcp/Node/dist/index.js` with your connection env vars, then call tools from your MCP-compatible client (Windsurf, Claude Desktop, etc.).

---

## Configuration

### Environment variables

| Variable | Required | Description |
| --- | --- | --- |
| `SERVER_NAME` | ✅ | SQL Server hostname or IP (e.g., `localhost`, `127.0.0.1`, `myserver.database.windows.net`) |
| `DATABASE_NAME` | ✅ | Target database name |
| `SQL_AUTH_MODE` | | Authentication mode: `sql`, `windows`, or `aad` (default: `aad`) |
| `SQL_USERNAME` | | Username for SQL or Windows auth |
| `SQL_PASSWORD` | | Password for SQL or Windows auth |
| `SQL_DOMAIN` | | Domain for Windows/NTLM auth (optional) |
| `SQL_PORT` | | Custom port (default: `1433`). Useful for named instances or Docker containers on non-standard ports. |
| `TRUST_SERVER_CERTIFICATE` | | Set to `true` for self-signed certificates or dev environments |
| `CONNECTION_TIMEOUT` | | Connection timeout in seconds (default: `30`) |
| `READONLY` | | Set to `true` to restrict to read-only tools (no INSERT, UPDATE, DROP) |
| `PROFILE_SAMPLE_SIZE_DEFAULT` | | Default sample size for `profile_table` (defaults to `50`, max `1000`) |
| `PROFILE_SAMPLE_RETURN_LIMIT` | | Max number of sample rows returned in responses (defaults to `10`, max `100`) |
| `SEARCH_SCHEMA_DEFAULT_LIMIT` | | Default row limit per section for `search_schema` pagination (defaults to `50`, max `200`) |

### Authentication modes

**SQL Server Authentication** (`SQL_AUTH_MODE=sql`)  
Standard username/password auth against SQL Server. Works with local instances, Docker containers, and Azure SQL with SQL auth enabled.

```json
"env": {
  "SERVER_NAME": "127.0.0.1",
  "DATABASE_NAME": "mydb",
  "SQL_AUTH_MODE": "sql",
  "SQL_USERNAME": "sa",
  "SQL_PASSWORD": "YourPassword123",
  "SQL_PORT": "1433",
  "TRUST_SERVER_CERTIFICATE": "true"
}
```

**Windows Authentication** (`SQL_AUTH_MODE=windows`)  
NTLM-based auth using domain credentials. Ideal for on-prem SQL Server in Active Directory environments.

```json
"env": {
  "SERVER_NAME": "sqlserver.corp.local",
  "DATABASE_NAME": "mydb",
  "SQL_AUTH_MODE": "windows",
  "SQL_USERNAME": "svc_account",
  "SQL_PASSWORD": "YourPassword123",
  "SQL_DOMAIN": "CORP"
}
```

**Azure AD Authentication** (`SQL_AUTH_MODE=aad` or omit)  
Interactive browser-based Azure AD authentication. Opens a browser window on first connection to authenticate. Best for Azure SQL Database with AAD-only auth.

```json
"env": {
  "SERVER_NAME": "myserver.database.windows.net",
  "DATABASE_NAME": "mydb",
  "SQL_AUTH_MODE": "aad"
}
```

### Multiple instances / Docker

If you're running multiple SQL Server instances (e.g., local dev on 1433, Docker on 1434), just change `SQL_PORT`:

```json
"env": {
  "SERVER_NAME": "127.0.0.1",
  "DATABASE_NAME": "devdb",
  "SQL_AUTH_MODE": "sql",
  "SQL_USERNAME": "sa",
  "SQL_PASSWORD": "DockerPassword123",
  "SQL_PORT": "1434",
  "TRUST_SERVER_CERTIFICATE": "true"
}
```

You can also run multiple instances of the MCP server in your config, each pointing to a different database or environment.

---

## ⚠️ Safety & Prudence

**With great power comes great responsibility.**

This MCP server exposes powerful CRUD operations including `drop_table`, `create_table`, `update_data`, and `insert_data`. If you enable full autopilot mode in your AI assistant—or simply aren't inspecting each tool call—you could trigger destructive actions you don't intend.

### Recommended Safeguards

1. **Use `READONLY=true` by default.** This globally disables all write operations, exposing only safe discovery tools. Enable write mode only when you explicitly need it.

2. **Be a prudent human in the loop.** Review tool calls before approval, especially for destructive operations. Don't blindly trust AI-generated queries against production data.

3. **Use dedicated credentials.** Connect with a database user that has only the permissions you intend to grant. Avoid `sa` or admin accounts in production.

4. **Test in non-production first.** Validate tool behavior against development or staging databases before pointing at production.

5. **Monitor and audit.** Enable SQL Server auditing or application-level logging to track executed queries.

The discovery tools (`search_schema`, `profile_table`, `inspect_relationships`) are designed to help you understand unfamiliar databases safely. Use them to build context before issuing any mutations.

### Tool tuning

- **Schema discovery pagination** – Use `SEARCH_SCHEMA_DEFAULT_LIMIT` to tune how many rows `search_schema` returns per page (default 50, max 200). Combine with `tableOffset`/`columnOffset` params for manual paging.
- **Profiling samples** – Use `PROFILE_SAMPLE_SIZE_DEFAULT` to control how many rows are sampled internally (default 50). Control how many rows are actually returned with `PROFILE_SAMPLE_RETURN_LIMIT` (default 10). Both respect per-request overrides via `sampleSize` and `includeSamples` args.

---

## Roadmap

Active development priorities:

1. **Sample row exposure** — Optional flag to return sampled records alongside `profile_table` stats
2. **Percentile metrics** — Median/p90 values for numeric columns via `PERCENTILE_CONT`
3. **Semantic intelligence layer** — Embedding-powered discovery for when you don't know what you're looking for
4. **Expanded auth options** — Continued hardening for enterprise SSO and managed identity scenarios

---

## Contributing

Contributions welcome. If you're working with SQL Server in environments where stability and security matter, I'd love to hear what tools would help you most.

**If this project is useful to you, consider giving it a ⭐ and sharing it with others who work with SQL Server.** The more eyes on it, the better it gets.

---

## License & attribution

MIT license, inherited from the upstream SQL-AI-samples project. Microsoft trademarks remain theirs.

Thanks to the Microsoft Azure SQL team for the original foundation, and to the MCP community for the protocol specs that make cross-agent tooling possible.
