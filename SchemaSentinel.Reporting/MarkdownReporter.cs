using System.Text;
using SchemaSentinel.Core.Comparison;

namespace SchemaSentinel.Reporting;

public class MarkdownReporter : IReportExporter
{
    public string FileExtension => ".md";
    public string DisplayName => "Markdown Report";

    public Task<string> GenerateAsync(ComparisonSummary summary, CancellationToken cancellationToken = default)
        => Task.FromResult(BuildMarkdown(summary));

    public async Task ExportAsync(ComparisonSummary summary, string filePath, CancellationToken cancellationToken = default)
    {
        var md = await GenerateAsync(summary, cancellationToken);
        await File.WriteAllTextAsync(filePath, md, Encoding.UTF8, cancellationToken);
    }

    private static string BuildMarkdown(ComparisonSummary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# SchemaSentinel — Schema Comparison Report");
        sb.AppendLine();
        sb.AppendLine($"- **Source:** {summary.SourceDescription}");
        sb.AppendLine($"- **Target:** {summary.TargetDescription}");
        sb.AppendLine($"- **Generated:** {summary.ComparedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Count |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Total Scanned | {summary.TotalScanned} |");
        sb.AppendLine($"| Identical | {summary.IdenticalCount} |");
        sb.AppendLine($"| Changed | {summary.ChangedCount} |");
        sb.AppendLine($"| Missing in Source | {summary.MissingInSourceCount} |");
        sb.AppendLine($"| Missing in Target | {summary.MissingInTargetCount} |");
        sb.AppendLine();

        var grouped = summary.Results
            .OrderBy(r => r.Status)
            .ThenBy(r => r.ObjectType)
            .ThenBy(r => r.FullName)
            .GroupBy(r => r.Status);

        foreach (var group in grouped)
        {
            sb.AppendLine($"## {StatusHeading(group.Key)}");
            sb.AppendLine();
            sb.AppendLine("| Type | Object | Details |");
            sb.AppendLine("|------|--------|---------|");

            foreach (var result in group)
            {
                var details = result.DetailedDifferences.Any()
                    ? string.Join("; ", result.DetailedDifferences)
                    : result.SummaryMessage;
                sb.AppendLine($"| {result.ObjectType} | `{result.FullName}` | {EscapeMd(details)} |");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string StatusHeading(DiffStatus status) => status switch
    {
        DiffStatus.Changed => "Changed Objects",
        DiffStatus.MissingInSource => "Missing in Source",
        DiffStatus.MissingInTarget => "Missing in Target",
        DiffStatus.Identical => "Identical Objects",
        _ => status.ToString()
    };

    private static string EscapeMd(string value) => value.Replace("|", "\\|");
}
