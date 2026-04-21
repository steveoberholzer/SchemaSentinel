using System.Text;
using SchemaSentinel.Core.Models;

namespace SchemaSentinel.Core.Normalization;

public class TableNormalizer
{
    public TableModel Normalize(TableModel table, ComparisonOptions options)
    {
        var columns = options.IgnoreColumnOrder
            ? table.Columns.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList()
            : table.Columns.OrderBy(c => c.OrdinalPosition).ToList();

        return new TableModel
        {
            SchemaName = table.SchemaName,
            TableName = table.TableName,
            ModifyDate = table.ModifyDate,
            PrimaryKey = table.PrimaryKey,
            Columns = columns
        };
    }

    public string RenderDefinition(TableModel table, ComparisonOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"TABLE [{table.SchemaName}].[{table.TableName}]");
        sb.AppendLine("(");

        var columns = options.SortColumnsAlphabetically
            ? table.Columns.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList()
            : table.Columns;

        foreach (var col in columns)
        {
            sb.Append($"    [{col.Name}] {FormatDataType(col)}");
            if (col.IsIdentity)
                sb.Append($" IDENTITY({col.IdentitySeed ?? 1},{col.IdentityIncrement ?? 1})");
            if (col.IsComputed && col.ComputedDefinition != null)
                sb.Append($" AS {col.ComputedDefinition}");
            if (col.DefaultDefinition != null)
                sb.Append($" DEFAULT {col.DefaultDefinition}");
            sb.AppendLine(col.IsNullable ? " NULL" : " NOT NULL");
        }

        if (table.PrimaryKey != null)
        {
            var pkType = table.PrimaryKey.IsClustered ? "CLUSTERED" : "NONCLUSTERED";
            var pkCols = string.Join(", ", table.PrimaryKey.Columns.Select(c => $"[{c}]"));
            sb.AppendLine($"    CONSTRAINT [{table.PrimaryKey.ConstraintName}] PRIMARY KEY {pkType} ({pkCols})");
        }

        sb.Append(")");
        return sb.ToString();
    }

    private static string FormatDataType(ColumnModel col) =>
        col.DataType.ToLowerInvariant() switch
        {
            "varchar" or "nvarchar" or "char" or "nchar" or "varbinary" or "binary" =>
                col.MaxLength == -1 ? $"{col.DataType}(MAX)" : $"{col.DataType}({col.MaxLength})",
            "decimal" or "numeric" =>
                $"{col.DataType}({col.Precision},{col.Scale})",
            _ => col.DataType
        };
}
