using SchemaSentinel.Core.Models;

namespace SchemaSentinel.Data.Queries;

internal static class TableQueries
{
    public static string GetTables(ComparisonOptions options)
    {
        var schemaFilter = BuildSchemaFilter(options.SchemaFilter, "s.name");

        return $@"
SELECT
    s.name  AS SchemaName,
    t.name  AS TableName,
    t.modify_date AS ModifyDate
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.is_ms_shipped = 0
{schemaFilter}
ORDER BY s.name, t.name";
    }

    public static string GetColumns(string schema, string tableName) => $@"
SELECT
    c.name        AS ColumnName,
    tp.name       AS DataType,
    CASE
        WHEN tp.name IN ('nvarchar','nchar','ntext') THEN c.max_length / 2
        ELSE c.max_length
    END           AS MaxLength,
    c.precision   AS Precision,
    c.scale       AS Scale,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity,
    ic.seed_value      AS IdentitySeed,
    ic.increment_value AS IdentityIncrement,
    c.is_computed AS IsComputed,
    cc.definition AS ComputedDefinition,
    dc.definition AS DefaultDefinition,
    c.column_id   AS OrdinalPosition
FROM sys.columns c
INNER JOIN sys.tables  t  ON c.object_id   = t.object_id
INNER JOIN sys.schemas s  ON t.schema_id   = s.schema_id
INNER JOIN sys.types   tp ON c.user_type_id = tp.user_type_id
LEFT  JOIN sys.identity_columns  ic ON c.object_id = ic.object_id AND c.column_id = ic.column_id
LEFT  JOIN sys.computed_columns  cc ON c.object_id = cc.object_id AND c.column_id = cc.column_id
LEFT  JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
WHERE s.name = '{EscapeSqlLiteral(schema)}'
  AND t.name = '{EscapeSqlLiteral(tableName)}'
ORDER BY c.column_id";

    public static string GetPrimaryKey(string schema, string tableName) => $@"
SELECT
    kc.name        AS ConstraintName,
    i.type_desc    AS IsClustered,
    c.name         AS ColumnName
FROM sys.key_constraints kc
INNER JOIN sys.tables       t  ON kc.parent_object_id = t.object_id
INNER JOIN sys.schemas      s  ON t.schema_id          = s.schema_id
INNER JOIN sys.indexes      i  ON kc.parent_object_id  = i.object_id AND kc.unique_index_id = i.index_id
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns      c  ON ic.object_id = c.object_id  AND ic.column_id = c.column_id
WHERE kc.type = 'PK'
  AND s.name = '{EscapeSqlLiteral(schema)}'
  AND t.name = '{EscapeSqlLiteral(tableName)}'
ORDER BY ic.key_ordinal";

    private static string BuildSchemaFilter(List<string> schemas, string columnExpr)
    {
        if (!schemas.Any()) return string.Empty;
        var list = string.Join(",", schemas.Select(s => $"'{EscapeSqlLiteral(s)}'"));
        return $"AND {columnExpr} IN ({list})";
    }

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''");
}
