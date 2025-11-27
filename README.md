# ConnorBritain / sql-server-mcp

This repo started as a fork of Microsoft’s [SQL-AI-samples](https://github.com/Azure-Samples/SQL-AI-samples). I have intentionally trimmed the sample sprawl down to focus on a **Model Context Protocol (MCP) server for Microsoft SQL Server** so I can ship the tooling I rely on for day-to-day database + AI workflows. Everything in this fork is geared toward making SQL Server AI automation possible, not hosting a gallery of notebooks.

## Upstream lineage & attribution

- ✅ Forked from Microsoft’s `SQL-AI-samples` under the MIT license.
- ✅ Retained the `MssqlMcp` project as the foundation.
- ✅ Removed the other Azure sample directories to keep the fork lean and purposeful.

If you need the full sample set, grab it from the Microsoft repo. If you want a productionized MSSQL MCP server with Connor’s custom tooling, stay here.

## What lives in this fork today

| Area | Highlights |
| --- | --- |
| **MCP Server (Node)** | `MssqlMcp/Node` exposes a suite of tools for SQL Server via MCP. The build is TypeScript (`tsc`) targeting Node 20. |
| **Authentication** | Supports SQL auth out of the box (env vars), plus Azure AD via `@azure/identity` when configured. |
| **Schema discovery** | New `search_schema` tool with pagination, fuzzy fallback (Levenshtein), and deterministic slices so large databases don’t flood transport. |
| **Classic CRUD helpers** | `read_data`, `insert_data`, `update_data`, `create_table`, `create_index`, `drop_table`, and `describe_table` remain available for agent-mode workflows. |

## Getting started

1. **Install deps**
   ```bash
   cd MssqlMcp/Node
   npm install
   npm run build
   ```
2. **Configure Windsurf / MCP client**
   Point your MCP config at `MssqlMcp/Node/dist/index.js` and supply the SQL auth env vars (see `mcp_config.json` snippet supplied in project docs).
3. **Run locally**
   ```bash
   npm start
   ```
4. **Call tools** via your MCP-compatible client (Windsurf, Claude Desktop, etc.).

## Tool catalog refresher

- `search_schema` – paginated table/column discovery plus fuzzy suggestions.
- `describe_table` – schema, data types, and (soon) FK metadata.
- `read_data` / `insert_data` / `update_data` / `drop_table` / `create_table` / `create_index` – standard SQL helpers.
- `list_tables` – lightweight inventory for quick prompts.

## Roadmap

1. **`profile_table` tool** – quick column “shape” insights (stats + samples).
2. **Relationship-aware `describe_table`** – include FK graph + backlinks to `search_schema` hits.
3. **Semantic discovery fork** – experimental embeddings/vector-powered exploration layered on top of this MCP core.

## License

This fork inherits the MIT license from the upstream SQL-AI-samples project. Microsoft trademarks remain theirs; references are purely for attribution.

## Acknowledgements

- Huge thanks to the Microsoft Azure SQL team for releasing SQL-AI-samples under MIT.
- Kudos to the MCP community for the SDKs and interop specs that make this server useful across agents.

---

Questions, bugs, or collaboration ideas? Open an issue or ping me directly. I’m actively iterating on this server to keep SQL Server-first AI tooling sharp.
