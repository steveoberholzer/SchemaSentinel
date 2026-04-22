using System.Text;
using SchemaSentinel.Core.Comparison;

namespace SchemaSentinel.Reporting;

public class SqlScriptReporter : IReportExporter
{
    public string FileExtension => ".sql";
    public string DisplayName => "SQL Script";

    public Task<string> GenerateAsync(ComparisonSummary summary, CancellationToken cancellationToken = default)
        => Task.FromResult(BuildScript(summary));

    public Task ExportAsync(ComparisonSummary summary, string filePath, CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(filePath, BuildScript(summary), Encoding.UTF8, cancellationToken);

    private static string BuildScript(ComparisonSummary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- ============================================================");
        sb.AppendLine("-- SchemaSentinel SQL Script");
        sb.AppendLine($"-- Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"-- Source:    {summary.SourceDescription}");
        sb.AppendLine($"-- Target:    {summary.TargetDescription}");
        sb.AppendLine("-- ============================================================");
        sb.AppendLine();

        var addColumns = summary.Results
            .Where(r => r.Status == DiffStatus.Changed && r.AlterScript != null)
            .ToList();
        var missingInTarget = summary.Results
            .Where(r => r.Status == DiffStatus.MissingInTarget)
            .ToList();

        if (addColumns.Any())
        {
            sb.AppendLine("-- ============================================================");
            sb.AppendLine("-- ADD MISSING COLUMNS TO TARGET");
            sb.AppendLine("-- ============================================================");
            sb.AppendLine();
            foreach (var r in addColumns)
            {
                sb.AppendLine($"-- {r.ObjectType}: {r.FullName}");
                sb.AppendLine(r.AlterScript);
            }
        }

        var missingModules = missingInTarget
            .Where(r => r.ObjectType != "Table" && r.SourceNormalizedDefinition != null)
            .ToList();
        if (missingModules.Any())
        {
            sb.AppendLine("-- ============================================================");
            sb.AppendLine("-- CREATE MISSING OBJECTS IN TARGET");
            sb.AppendLine("-- ============================================================");
            sb.AppendLine();
            foreach (var r in missingModules)
            {
                sb.AppendLine($"-- {r.ObjectType}: {r.FullName}");
                sb.AppendLine(r.SourceNormalizedDefinition);
                sb.AppendLine("GO");
                sb.AppendLine();
            }
        }

        var missingTables = missingInTarget.Where(r => r.ObjectType == "Table").ToList();
        if (missingTables.Any())
        {
            sb.AppendLine("-- ============================================================");
            sb.AppendLine("-- TABLES MISSING IN TARGET (manual CREATE TABLE required)");
            sb.AppendLine("-- ============================================================");
            sb.AppendLine();
            foreach (var r in missingTables)
                sb.AppendLine($"-- {r.FullName}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
