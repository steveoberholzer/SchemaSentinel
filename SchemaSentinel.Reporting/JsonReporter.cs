using System.Text.Json;
using System.Text.Json.Serialization;
using SchemaSentinel.Core.Comparison;

namespace SchemaSentinel.Reporting;

public class JsonReporter : IReportExporter
{
    public string FileExtension => ".json";
    public string DisplayName => "JSON Report";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task ExportAsync(ComparisonSummary summary, string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, summary, Options, cancellationToken);
    }
}
