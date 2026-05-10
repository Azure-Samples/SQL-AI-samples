// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Xml.Linq;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    private static readonly XNamespace ShowplanNs = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";

    [McpServerTool(
        Title = "Explain Query",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Returns the estimated execution plan (showplan XML) for a query without running it, plus an extracted summary of total cost, top operators and missing-index hints.")]
    public Task<DbOperationResult> ExplainQuery(
        [Description("SQL query to analyze. Use @paramName for parameters.")] string sql,
        [Description("Optional named parameters bound to @paramName placeholders")] Dictionary<string, object?>? parameters = null,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(async (conn, ct) =>
        {
            using (var setOn = CreateCommand("SET SHOWPLAN_XML ON", conn))
            {
                await setOn.ExecuteNonQueryAsync(ct);
            }

            string? planXml = null;
            try
            {
                using var cmd = CreateCommand(sql, conn);
                SqlParameterBinding.Apply(cmd, parameters);
                using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    planXml = reader.GetString(0);
                }
            }
            finally
            {
                using var setOff = CreateCommand("SET SHOWPLAN_XML OFF", conn);
                await setOff.ExecuteNonQueryAsync(ct);
            }

            if (string.IsNullOrEmpty(planXml))
            {
                return DbOperationResult.Fail("No execution plan returned.");
            }

            var summary = ExtractPlanSummary(planXml);
            return DbOperationResult.Ok(new { plan_xml = planXml, summary });
        }, database, cancellationToken);
    }

    private static object ExtractPlanSummary(string planXml)
    {
        try
        {
            var doc = XDocument.Parse(planXml);

            var stmts = doc.Descendants(ShowplanNs + "StmtSimple")
                .Select(s => new
                {
                    statement = (string?)s.Attribute("StatementText"),
                    estimated_rows = ParseDouble(s.Attribute("StatementEstRows")?.Value),
                    estimated_subtree_cost = ParseDouble(s.Attribute("StatementSubTreeCost")?.Value),
                    optimization_level = (string?)s.Attribute("StatementOptmLevel"),
                })
                .ToList();

            var topOperators = doc.Descendants(ShowplanNs + "RelOp")
                .OrderByDescending(o => ParseDouble(o.Attribute("EstimatedTotalSubtreeCost")?.Value) ?? 0d)
                .Take(5)
                .Select(o => new
                {
                    physical_op = (string?)o.Attribute("PhysicalOp"),
                    logical_op = (string?)o.Attribute("LogicalOp"),
                    estimated_rows = ParseDouble(o.Attribute("EstimateRows")?.Value),
                    estimated_total_subtree_cost = ParseDouble(o.Attribute("EstimatedTotalSubtreeCost")?.Value),
                })
                .ToList();

            var missingIndexes = doc.Descendants(ShowplanNs + "MissingIndexGroup")
                .Select(mig =>
                {
                    var impact = ParseDouble(mig.Attribute("Impact")?.Value);
                    var mi = mig.Element(ShowplanNs + "MissingIndex");
                    var dbName = (string?)mi?.Attribute("Database");
                    var schemaName = (string?)mi?.Attribute("Schema");
                    var tableName = (string?)mi?.Attribute("Table");

                    var equality = mi?.Elements(ShowplanNs + "ColumnGroup")
                        .Where(g => (string?)g.Attribute("Usage") == "EQUALITY")
                        .SelectMany(g => g.Elements(ShowplanNs + "Column").Select(c => (string?)c.Attribute("Name")))
                        .Where(n => n is not null)
                        .ToList() ?? new();
                    var inequality = mi?.Elements(ShowplanNs + "ColumnGroup")
                        .Where(g => (string?)g.Attribute("Usage") == "INEQUALITY")
                        .SelectMany(g => g.Elements(ShowplanNs + "Column").Select(c => (string?)c.Attribute("Name")))
                        .Where(n => n is not null)
                        .ToList() ?? new();
                    var include = mi?.Elements(ShowplanNs + "ColumnGroup")
                        .Where(g => (string?)g.Attribute("Usage") == "INCLUDE")
                        .SelectMany(g => g.Elements(ShowplanNs + "Column").Select(c => (string?)c.Attribute("Name")))
                        .Where(n => n is not null)
                        .ToList() ?? new();

                    return new
                    {
                        impact,
                        database = dbName,
                        schema = schemaName,
                        table = tableName,
                        equality_columns = equality,
                        inequality_columns = inequality,
                        include_columns = include,
                    };
                })
                .OrderByDescending(x => x.impact ?? 0)
                .ToList();

            var warnings = doc.Descendants(ShowplanNs + "Warnings")
                .SelectMany(w => w.Elements())
                .Select(w => w.Name.LocalName)
                .Distinct()
                .ToList();

            return new
            {
                statements = stmts,
                top_operators = topOperators,
                missing_indexes = missingIndexes,
                warnings,
            };
        }
        catch (Exception ex)
        {
            return new { parse_error = ex.Message };
        }
    }

    private static double? ParseDouble(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
}
