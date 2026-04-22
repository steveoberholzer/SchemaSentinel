using System.Net;
using System.Text;
using SchemaSentinel.Core.Comparison;

namespace SchemaSentinel.Reporting;

public class HtmlReporter : IReportExporter
{
    public string FileExtension => ".html";
    public string DisplayName => "HTML Report";

    public Task<string> GenerateAsync(ComparisonSummary summary, CancellationToken cancellationToken = default)
        => Task.FromResult(BuildHtml(summary));

    public async Task ExportAsync(ComparisonSummary summary, string filePath, CancellationToken cancellationToken = default)
    {
        var html = await GenerateAsync(summary, cancellationToken);
        await File.WriteAllTextAsync(filePath, html, Encoding.UTF8, cancellationToken);
    }

    private static string BuildHtml(ComparisonSummary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>SchemaSentinel Report</title>
                <style>
                    body { font-family: Segoe UI, Arial, sans-serif; margin: 0; padding: 0; background: #f5f5f5; color: #333; }
                    .header { background: #1A3A5C; color: white; padding: 20px 30px; }
                    .header h1 { margin: 0; font-size: 24px; }
                    .header .subtitle { margin: 4px 0 0; font-size: 13px; opacity: 0.8; }
                    .container { max-width: 1400px; margin: 0 auto; padding: 20px 30px; }
                    .summary { display: flex; gap: 16px; margin-bottom: 24px; flex-wrap: wrap; }
                    .summary-card { background: white; border-radius: 6px; padding: 16px 24px; min-width: 140px; box-shadow: 0 1px 4px rgba(0,0,0,.1); }
                    .summary-card .count { font-size: 32px; font-weight: 700; }
                    .summary-card .label { font-size: 12px; color: #666; margin-top: 2px; }
                    .c-identical { color: #27AE60; } .c-changed { color: #E67E22; }
                    .c-missing-src { color: #E74C3C; } .c-missing-tgt { color: #C0392B; }
                    .c-total { color: #1A3A5C; }
                    table { width: 100%; border-collapse: collapse; background: white; border-radius: 6px; overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,.1); }
                    th { background: #1A3A5C; color: white; text-align: left; padding: 10px 14px; font-size: 13px; }
                    td { padding: 9px 14px; border-bottom: 1px solid #eee; font-size: 13px; vertical-align: top; }
                    tr:last-child td { border-bottom: none; }
                    tr:hover td { background: #fafafa; }
                    .badge { display: inline-block; padding: 2px 10px; border-radius: 20px; font-size: 11px; font-weight: 600; }
                    .badge-identical { background: #EAFAF1; color: #27AE60; }
                    .badge-changed { background: #FEF9E7; color: #E67E22; }
                    .badge-missing-src { background: #FDEDEC; color: #E74C3C; }
                    .badge-missing-tgt { background: #FDEDEC; color: #C0392B; }
                    .diffs { font-size: 12px; color: #555; margin: 0; padding-left: 18px; }
                    .diffs li { margin: 2px 0; }
                    .section-title { font-size: 18px; font-weight: 600; margin: 28px 0 12px; color: #1A3A5C; }
                    .meta { font-size: 12px; color: #888; margin-bottom: 20px; }
                </style>
            </head>
            <body>
            """);

        sb.AppendLine($"""
            <div class="header">
                <h1>SchemaSentinel — Schema Comparison Report</h1>
                <p class="subtitle">Source: {H(summary.SourceDescription)} &nbsp;|&nbsp; Target: {H(summary.TargetDescription)} &nbsp;|&nbsp; Generated: {summary.ComparedAt:yyyy-MM-dd HH:mm:ss} UTC</p>
            </div>
            <div class="container">
            """);

        sb.AppendLine($"""
            <div class="summary">
                <div class="summary-card"><div class="count c-total">{summary.TotalScanned}</div><div class="label">Total Scanned</div></div>
                <div class="summary-card"><div class="count c-identical">{summary.IdenticalCount}</div><div class="label">Identical</div></div>
                <div class="summary-card"><div class="count c-changed">{summary.ChangedCount}</div><div class="label">Changed</div></div>
                <div class="summary-card"><div class="count c-missing-src">{summary.MissingInSourceCount}</div><div class="label">Missing in Source</div></div>
                <div class="summary-card"><div class="count c-missing-tgt">{summary.MissingInTargetCount}</div><div class="label">Missing in Target</div></div>
            </div>
            """);

        sb.AppendLine("""<div class="section-title">Results</div>""");
        sb.AppendLine("""
            <table>
                <thead>
                    <tr>
                        <th>Type</th><th>Schema</th><th>Object</th><th>Status</th><th>Details</th>
                    </tr>
                </thead>
                <tbody>
            """);

        foreach (var result in summary.Results.OrderBy(r => r.Status).ThenBy(r => r.ObjectType).ThenBy(r => r.FullName))
        {
            var (badgeClass, statusText) = result.Status switch
            {
                DiffStatus.Identical => ("badge-identical", "Identical"),
                DiffStatus.Changed => ("badge-changed", "Changed"),
                DiffStatus.MissingInSource => ("badge-missing-src", "Missing in Source"),
                DiffStatus.MissingInTarget => ("badge-missing-tgt", "Missing in Target"),
                _ => ("", result.Status.ToString())
            };

            var diffs = result.DetailedDifferences.Any()
                ? $"<ul class='diffs'>{string.Join("", result.DetailedDifferences.Select(d => $"<li>{H(d)}</li>"))}</ul>"
                : H(result.SummaryMessage);

            sb.AppendLine($"""
                    <tr>
                        <td>{H(result.ObjectType)}</td>
                        <td>{H(result.SchemaName)}</td>
                        <td>{H(result.ObjectName)}</td>
                        <td><span class="badge {badgeClass}">{statusText}</span></td>
                        <td>{diffs}</td>
                    </tr>
                """);
        }

        sb.AppendLine("</tbody></table>");
        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
