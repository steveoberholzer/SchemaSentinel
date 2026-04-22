namespace SchemaSentinel.Core.Comparison;

public class ObjectComparisonResult
{
    public string ObjectType { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public DiffStatus Status { get; set; }
    public string SummaryMessage { get; set; } = string.Empty;
    public List<string> DetailedDifferences { get; set; } = new();
    public string? SourceNormalizedDefinition { get; set; }
    public string? TargetNormalizedDefinition { get; set; }
    public string? SourceRawDefinition { get; set; }
    public string? TargetRawDefinition { get; set; }
    public string? AlterScript { get; set; }
    public DateTime? SourceModifyDate { get; set; }
    public DateTime? TargetModifyDate { get; set; }

    public string FullName => $"{SchemaName}.{ObjectName}";
}
