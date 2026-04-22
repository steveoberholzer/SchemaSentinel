using System.Text;
using SchemaSentinel.Core.Models;
using SchemaSentinel.Core.Normalization;

namespace SchemaSentinel.Core.Comparison;

public class SchemaComparer
{
    private readonly TableNormalizer _tableNormalizer = new();
    private readonly ModuleNormalizer _moduleNormalizer = new();

    public ComparisonSummary Compare(
        IList<TableModel> sourceTables,
        IList<TableModel> targetTables,
        IList<SqlModuleModel> sourceModules,
        IList<SqlModuleModel> targetModules,
        ComparisonOptions options)
    {
        var results = new List<ObjectComparisonResult>();

        if (options.CompareTables)
            results.AddRange(CompareTables(sourceTables, targetTables, options));
        if (options.CompareViews)
            results.AddRange(CompareModules(
                sourceModules.Where(m => m.ModuleType == ModuleType.View).ToList(),
                targetModules.Where(m => m.ModuleType == ModuleType.View).ToList(),
                options));
        if (options.CompareProcedures)
            results.AddRange(CompareModules(
                sourceModules.Where(m => m.ModuleType == ModuleType.Procedure).ToList(),
                targetModules.Where(m => m.ModuleType == ModuleType.Procedure).ToList(),
                options));
        if (options.CompareFunctions)
            results.AddRange(CompareModules(
                sourceModules.Where(m => m.ModuleType == ModuleType.Function).ToList(),
                targetModules.Where(m => m.ModuleType == ModuleType.Function).ToList(),
                options));

        return new ComparisonSummary
        {
            Results = results,
            TotalScanned = results.Count,
            IdenticalCount = results.Count(r => r.Status == DiffStatus.Identical),
            ChangedCount = results.Count(r => r.Status == DiffStatus.Changed),
            MissingInSourceCount = results.Count(r => r.Status == DiffStatus.MissingInSource),
            MissingInTargetCount = results.Count(r => r.Status == DiffStatus.MissingInTarget)
        };
    }

    private IEnumerable<ObjectComparisonResult> CompareTables(
        IList<TableModel> source, IList<TableModel> target, ComparisonOptions options)
    {
        var sourceDict = source.ToDictionary(t => t.FullName.ToLowerInvariant());
        var targetDict = target.ToDictionary(t => t.FullName.ToLowerInvariant());

        foreach (var key in sourceDict.Keys.Union(targetDict.Keys))
        {
            var hasSource = sourceDict.TryGetValue(key, out var src);
            var hasTarget = targetDict.TryGetValue(key, out var tgt);

            if (!hasSource)
            {
                var normTgtOnly = _tableNormalizer.Normalize(tgt!, options);
                yield return Missing("Table", tgt!.SchemaName, tgt.TableName, DiffStatus.MissingInSource,
                    tgt.ModifyDate,
                    targetDef: _tableNormalizer.RenderDefinition(normTgtOnly, options),
                    targetRawDef: _tableNormalizer.RenderRaw(tgt));
                continue;
            }
            if (!hasTarget)
            {
                var normSrcOnly = _tableNormalizer.Normalize(src!, options);
                yield return Missing("Table", src!.SchemaName, src.TableName, DiffStatus.MissingInTarget,
                    src.ModifyDate,
                    sourceDef: _tableNormalizer.RenderDefinition(normSrcOnly, options),
                    sourceRawDef: _tableNormalizer.RenderRaw(src!));
                continue;
            }

            var normSrc = _tableNormalizer.Normalize(src!, options);
            var normTgt = _tableNormalizer.Normalize(tgt!, options);
            var diffs = DiffTables(normSrc, normTgt);
            var alterScript = BuildAlterScript(src!, tgt!);

            yield return new ObjectComparisonResult
            {
                ObjectType = "Table",
                SchemaName = src!.SchemaName,
                ObjectName = src.TableName,
                Status = diffs.Any() ? DiffStatus.Changed : DiffStatus.Identical,
                SummaryMessage = diffs.Any() ? $"{diffs.Count} difference(s) found." : "Identical.",
                DetailedDifferences = diffs,
                SourceNormalizedDefinition = _tableNormalizer.RenderDefinition(normSrc, options),
                TargetNormalizedDefinition = _tableNormalizer.RenderDefinition(normTgt, options),
                SourceRawDefinition = _tableNormalizer.RenderRaw(src!),
                TargetRawDefinition = _tableNormalizer.RenderRaw(tgt!),
                AlterScript = alterScript.Length > 0 ? alterScript : null,
                SourceModifyDate = src.ModifyDate,
                TargetModifyDate = tgt!.ModifyDate,
            };
        }
    }

    private IEnumerable<ObjectComparisonResult> CompareModules(
        IList<SqlModuleModel> source, IList<SqlModuleModel> target, ComparisonOptions options)
    {
        var sourceDict = source.ToDictionary(m => m.FullName.ToLowerInvariant());
        var targetDict = target.ToDictionary(m => m.FullName.ToLowerInvariant());

        foreach (var key in sourceDict.Keys.Union(targetDict.Keys))
        {
            var hasSource = sourceDict.TryGetValue(key, out var src);
            var hasTarget = targetDict.TryGetValue(key, out var tgt);
            var objectType = (hasSource ? src! : tgt!).ModuleType.ToString();

            if (!hasSource)
            {
                yield return Missing(objectType, tgt!.SchemaName, tgt.ObjectName, DiffStatus.MissingInSource,
                    tgt.ModifyDate,
                    targetDef: _moduleNormalizer.Normalize(tgt.Definition, options),
                    targetRawDef: tgt.Definition);
                continue;
            }
            if (!hasTarget)
            {
                yield return Missing(objectType, src!.SchemaName, src.ObjectName, DiffStatus.MissingInTarget,
                    src.ModifyDate,
                    sourceDef: _moduleNormalizer.Normalize(src.Definition, options),
                    sourceRawDef: src.Definition);
                continue;
            }

            var normSrcDef = _moduleNormalizer.Normalize(src!.Definition, options);
            var normTgtDef = _moduleNormalizer.Normalize(tgt!.Definition, options);
            var comparison = options.IgnoreCasing
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            var isDifferent = !string.Equals(normSrcDef, normTgtDef, comparison);

            yield return new ObjectComparisonResult
            {
                ObjectType = objectType,
                SchemaName = src.SchemaName,
                ObjectName = src.ObjectName,
                Status = isDifferent ? DiffStatus.Changed : DiffStatus.Identical,
                SummaryMessage = isDifferent ? "Definition differs between source and target." : "Identical.",
                SourceNormalizedDefinition = normSrcDef,
                TargetNormalizedDefinition = normTgtDef,
                SourceRawDefinition = src.Definition,
                TargetRawDefinition = tgt.Definition,
                SourceModifyDate = src.ModifyDate,
                TargetModifyDate = tgt.ModifyDate,
            };
        }
    }

    public const string TargetOnlyColumnMarker = "[target-only] ";

    private static List<string> DiffTables(TableModel source, TableModel target)
    {
        var diffs = new List<string>();
        var srcCols = source.Columns.ToDictionary(c => c.Name.ToLowerInvariant());
        var tgtCols = target.Columns.ToDictionary(c => c.Name.ToLowerInvariant());

        foreach (var col in srcCols.Keys.Except(tgtCols.Keys))
            diffs.Add($"Column '{srcCols[col].Name}' exists in source but not in target.");
        foreach (var col in tgtCols.Keys.Except(srcCols.Keys))
            diffs.Add($"{TargetOnlyColumnMarker}Column '{tgtCols[col].Name}' exists in target but not in source.");

        foreach (var col in srcCols.Keys.Intersect(tgtCols.Keys))
        {
            var s = srcCols[col];
            var t = tgtCols[col];
            if (s.DataType != t.DataType)
                diffs.Add($"Column '{s.Name}': data type differs (source: {s.DataType}, target: {t.DataType}).");
            if (s.MaxLength != t.MaxLength)
                diffs.Add($"Column '{s.Name}': max length differs (source: {s.MaxLength}, target: {t.MaxLength}).");
            if (s.Precision != t.Precision)
                diffs.Add($"Column '{s.Name}': precision differs (source: {s.Precision}, target: {t.Precision}).");
            if (s.Scale != t.Scale)
                diffs.Add($"Column '{s.Name}': scale differs (source: {s.Scale}, target: {t.Scale}).");
            if (s.IsNullable != t.IsNullable)
                diffs.Add($"Column '{s.Name}': nullability differs (source: {(s.IsNullable ? "NULL" : "NOT NULL")}, target: {(t.IsNullable ? "NULL" : "NOT NULL")}).");
            if (s.IsIdentity != t.IsIdentity)
                diffs.Add($"Column '{s.Name}': identity property differs (source: {s.IsIdentity}, target: {t.IsIdentity}).");
        }

        if (source.PrimaryKey == null && target.PrimaryKey != null)
            diffs.Add("Primary key exists in target but not in source.");
        else if (source.PrimaryKey != null && target.PrimaryKey == null)
            diffs.Add("Primary key exists in source but not in target.");
        else if (source.PrimaryKey != null && target.PrimaryKey != null)
        {
            var srcPk = string.Join(",", source.PrimaryKey.Columns.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
            var tgtPk = string.Join(",", target.PrimaryKey.Columns.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
            if (!string.Equals(srcPk, tgtPk, StringComparison.OrdinalIgnoreCase))
                diffs.Add($"Primary key columns differ (source: [{srcPk}], target: [{tgtPk}]).");
            if (source.PrimaryKey.IsClustered != target.PrimaryKey.IsClustered)
                diffs.Add($"Primary key clustering differs (source: {(source.PrimaryKey.IsClustered ? "CLUSTERED" : "NONCLUSTERED")}, target: {(target.PrimaryKey.IsClustered ? "CLUSTERED" : "NONCLUSTERED")}).");
        }

        return diffs;
    }

    private static string BuildAlterScript(TableModel source, TableModel target)
    {
        var tgtCols = new HashSet<string>(target.Columns.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        foreach (var col in source.Columns.Where(c => !tgtCols.Contains(c.Name)))
        {
            var typePart = TableNormalizer.FormatDataType(col);
            var identity = col.IsIdentity ? $" IDENTITY({col.IdentitySeed ?? 1},{col.IdentityIncrement ?? 1})" : string.Empty;
            var defaultPart = col.DefaultDefinition != null ? $" DEFAULT {col.DefaultDefinition}" : string.Empty;
            var nullable = col.IsNullable ? "NULL" : "NOT NULL";
            sb.AppendLine($"ALTER TABLE [{source.SchemaName}].[{source.TableName}] ADD [{col.Name}] {typePart}{identity} {nullable}{defaultPart};");
        }
        return sb.ToString();
    }

    private static ObjectComparisonResult Missing(
        string type, string schema, string name, DiffStatus status,
        DateTime? modifyDate = null,
        string? sourceDef = null, string? targetDef = null,
        string? sourceRawDef = null, string? targetRawDef = null) =>
        new()
        {
            ObjectType = type,
            SchemaName = schema,
            ObjectName = name,
            Status = status,
            SummaryMessage = status == DiffStatus.MissingInSource
                ? $"{type} exists in target but not in source."
                : $"{type} exists in source but not in target.",
            SourceNormalizedDefinition = sourceDef,
            TargetNormalizedDefinition = targetDef,
            SourceRawDefinition = sourceRawDef,
            TargetRawDefinition = targetRawDef,
            SourceModifyDate = status == DiffStatus.MissingInTarget ? modifyDate : null,
            TargetModifyDate = status == DiffStatus.MissingInSource ? modifyDate : null,
        };
}
