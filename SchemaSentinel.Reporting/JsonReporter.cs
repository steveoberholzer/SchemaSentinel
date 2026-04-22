using System.Text;
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

    public Task<string> GenerateAsync(ComparisonSummary summary, CancellationToken cancellationToken = default)
        => Task.FromResult(JsonSerializer.Serialize(summary, Options));

    public async Task ExportAsync(ComparisonSummary summary, string filePath, CancellationToken cancellationToken = default)
    {
        var json = await GenerateAsync(summary, cancellationToken);
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8, cancellationToken);
    }
}
