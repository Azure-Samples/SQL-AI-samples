// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Generate POCO",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Generates a C# POCO class from a table's schema, ready to drop into an EF Core or Dapper project. Maps SQL types to CLR types, marks PK with [Key], adds [Column] for non-default mappings, and uses nullable reference types.")]
    public Task<DbOperationResult> GeneratePoco(
        [Description("Table name (optionally schema-qualified, e.g. 'dbo.Users')")] string name,
        [Description("Optional namespace for the generated class. Default: 'YourApp.Models'.")] string? @namespace = null,
        [Description("Optional class name override. Default: PascalCased table name.")] string? className = null,
        [Description("Optional database name to override the connection's default catalog")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        string? schema = null;
        if (name.Contains('.'))
        {
            var parts = name.Split('.', 2);
            schema = parts[0];
            name = parts[1];
        }

        var deny = CheckSchemaAllowed(schema);
        if (deny is not null) return Task.FromResult(deny);

        return ExecuteAsync(async (conn, ct) =>
        {
            var info = await FetchTableSchemaAsync(conn, schema, name, ct);
            if (info is null)
            {
                return DbOperationResult.Fail($"Table '{name}' not found.");
            }

            var ns = string.IsNullOrWhiteSpace(@namespace) ? "YourApp.Models" : @namespace!;
            var clsName = string.IsNullOrWhiteSpace(className) ? ToPascal(info.Name) : className!;
            var pkSet = new HashSet<string>(info.PrimaryKey, StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine();
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();

            var tableAttr = info.Schema == "dbo"
                ? $"[Table(\"{info.Name}\")]"
                : $"[Table(\"{info.Name}\", Schema = \"{info.Schema}\")]";
            sb.AppendLine(tableAttr);
            sb.AppendLine($"public class {clsName}");
            sb.AppendLine("{");

            for (var i = 0; i < info.Columns.Count; i++)
            {
                var c = info.Columns[i];
                var clrType = MapSqlTypeToCSharp(c.SqlType, c.Nullable);
                var propName = ToPascal(c.Name);

                if (pkSet.Contains(c.Name))
                {
                    sb.AppendLine("    [Key]");
                }
                if (c.IsIdentity)
                {
                    sb.AppendLine("    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
                }
                else if (c.IsComputed)
                {
                    sb.AppendLine("    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]");
                }

                if (!c.Nullable && clrType == "string")
                {
                    sb.AppendLine("    [Required]");
                }

                if (clrType.StartsWith("string") && c.MaxLength > 0)
                {
                    var len = c.SqlType.StartsWith("n", StringComparison.OrdinalIgnoreCase) ? c.MaxLength / 2 : c.MaxLength;
                    if (len > 0)
                    {
                        sb.AppendLine($"    [MaxLength({len})]");
                    }
                }

                if (!string.Equals(c.Name, propName, StringComparison.Ordinal))
                {
                    sb.AppendLine($"    [Column(\"{c.Name}\")]");
                }

                sb.Append($"    public {clrType} {propName} {{ get; set; }}");
                if (clrType == "string" && !c.Nullable)
                {
                    sb.Append(" = string.Empty;");
                }
                sb.AppendLine();
                if (i < info.Columns.Count - 1)
                {
                    sb.AppendLine();
                }
            }

            sb.AppendLine("}");

            return DbOperationResult.Ok(new
            {
                @namespace = ns,
                class_name = clsName,
                source = sb.ToString(),
            });
        }, database, cancellationToken);
    }

    private static string ToPascal(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var parts = s.Split('_', '-', ' ');
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1) sb.Append(part[1..]);
        }
        return sb.ToString();
    }
}
