using SchemaSentinel.Core.Comparison;

namespace SchemaSentinel.Reporting;

public interface IReportExporter
{
    string FileExtension { get; }
    string DisplayName { get; }
    Task ExportAsync(ComparisonSummary summary, string filePath, CancellationToken cancellationToken = default);
}
