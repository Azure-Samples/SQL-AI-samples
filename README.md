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
| **Flexible authentication** | SQL auth, Windows auth, and Azure AD—not just Azure AD callback. Configure what fits your environment. |
| **Safe data operations** | `read_data`, `insert_data`, `update_data`, `describe_table`, `list_tables`, plus DDL helpers (`create_table`, `create_index`, `drop_table`) for agent-mode workflows. |
| **MCP-native** | Works with Windsurf, Claude Desktop, and any MCP-compatible client out of the box. |

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

## Roadmap

Active development priorities:

1. **`profile_table`** — Column-level stats, sample values, and data shape insights before you write a single query
2. **Relationship-aware metadata** — FK graphs and cross-references baked into `describe_table` and `search_schema`
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
