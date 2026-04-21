using SchemaSentinel.Core.Models;

namespace SchemaSentinel.Data.Queries;

internal static class ModuleQueries
{
    public static string GetModules(ComparisonOptions options, ModuleType? moduleType = null)
    {
        var typeFilter = moduleType switch
        {
            ModuleType.View      => "AND o.type = 'V'",
            ModuleType.Procedure => "AND o.type = 'P'",
            ModuleType.Function  => "AND o.type IN ('FN','IF','TF')",
            _                    => "AND o.type IN ('V','P','FN','IF','TF')"
        };

        var changedSince = options.ChangedSince.HasValue
            ? $"AND o.modify_date >= '{options.ChangedSince.Value:yyyy-MM-dd}'"
            : string.Empty;

        var schemaFilter = BuildSchemaFilter(options.SchemaFilter, "s.name");

        return $@"
SELECT
    s.name       AS SchemaName,
    o.name       AS ObjectName,
    o.type       AS ObjectType,
    m.definition AS Definition,
    o.modify_date AS ModifyDate
FROM sys.objects o
INNER JOIN sys.schemas    s ON o.schema_id  = s.schema_id
INNER JOIN sys.sql_modules m ON o.object_id = m.object_id
WHERE o.is_ms_shipped = 0
{typeFilter}
{changedSince}
{schemaFilter}
ORDER BY s.name, o.name";
    }

    private static string BuildSchemaFilter(List<string> schemas, string columnExpr)
    {
        if (!schemas.Any()) return string.Empty;
        var list = string.Join(",", schemas.Select(s => $"'{EscapeSqlLiteral(s)}'"));
        return $"AND {columnExpr} IN ({list})";
    }

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''");
}
