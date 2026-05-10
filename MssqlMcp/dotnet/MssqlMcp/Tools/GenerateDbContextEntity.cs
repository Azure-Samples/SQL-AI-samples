// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace Mssql.McpServer;

public partial class Tools
{
    [McpServerTool(
        Title = "Generate DbContext Entity",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Generates an EF Core Fluent API configuration snippet (modelBuilder.Entity<T>(...) block) for a table. Drop the snippet into your DbContext.OnModelCreating override.")]
    public Task<DbOperationResult> GenerateDbContextEntity(
        [Description("Table name (optionally schema-qualified, e.g. 'dbo.Users')")] string name,
        [Description("Optional CLR class name. Default: PascalCased table name.")] string? className = null,
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

            var clsName = string.IsNullOrWhiteSpace(className) ? ToPascal(info.Name) : className!;
            var sb = new StringBuilder();
            sb.AppendLine($"modelBuilder.Entity<{clsName}>(b =>");
            sb.AppendLine("{");
            sb.AppendLine(info.Schema == "dbo"
                ? $"    b.ToTable(\"{info.Name}\");"
                : $"    b.ToTable(\"{info.Name}\", \"{info.Schema}\");");

            if (info.PrimaryKey.Count == 1)
            {
                sb.AppendLine($"    b.HasKey(x => x.{ToPascal(info.PrimaryKey[0])});");
            }
            else if (info.PrimaryKey.Count > 1)
            {
                var keyExpr = string.Join(", ", info.PrimaryKey.Select(k => $"x.{ToPascal(k)}"));
                sb.AppendLine($"    b.HasKey(x => new {{ {keyExpr} }});");
            }

            foreach (var c in info.Columns)
            {
                var prop = ToPascal(c.Name);
                var line = new StringBuilder();
                line.Append($"    b.Property(x => x.{prop})");
                if (!string.Equals(c.Name, prop, StringComparison.Ordinal))
                {
                    line.Append($".HasColumnName(\"{c.Name}\")");
                }

                var typeName = BuildSqlTypeName(c);
                if (!string.IsNullOrEmpty(typeName))
                {
                    line.Append($".HasColumnType(\"{typeName}\")");
                }

                if (!c.Nullable)
                {
                    line.Append(".IsRequired()");
                }

                if (c.IsIdentity)
                {
                    line.Append(".ValueGeneratedOnAdd().UseIdentityColumn()");
                }
                else if (c.IsComputed)
                {
                    line.Append(".ValueGeneratedOnAddOrUpdate()");
                }

                if (IsStringSqlType(c.SqlType) && c.MaxLength > 0)
                {
                    var len = c.SqlType.StartsWith("n", StringComparison.OrdinalIgnoreCase) ? c.MaxLength / 2 : c.MaxLength;
                    if (len > 0)
                    {
                        line.Append($".HasMaxLength({len})");
                    }
                }

                line.Append(';');
                sb.AppendLine(line.ToString());
            }

            sb.AppendLine("});");

            return DbOperationResult.Ok(new
            {
                class_name = clsName,
                snippet = sb.ToString(),
            });
        }, database, cancellationToken);
    }

    private static bool IsStringSqlType(string sqlType) => sqlType.ToLowerInvariant() switch
    {
        "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext" or "sysname" => true,
        _ => false,
    };

    private static string? BuildSqlTypeName(ColumnInfo c)
    {
        var t = c.SqlType.ToLowerInvariant();
        return t switch
        {
            "decimal" or "numeric" => $"{t}({c.Precision},{c.Scale})",
            "datetime2" or "time" or "datetimeoffset" => c.Scale > 0 ? $"{t}({c.Scale})" : t,
            "varchar" or "char" => c.MaxLength == -1 ? $"{t}(max)" : $"{t}({c.MaxLength})",
            "nvarchar" or "nchar" => c.MaxLength == -1 ? $"{t}(max)" : $"{t}({c.MaxLength / 2})",
            "varbinary" or "binary" => c.MaxLength == -1 ? $"{t}(max)" : $"{t}({c.MaxLength})",
            _ => null,
        };
    }
}
