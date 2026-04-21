namespace SchemaSentinel.Core.Comparison;

public class ComparisonSummary
{
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
    public string SourceDescription { get; set; } = string.Empty;
    public string TargetDescription { get; set; } = string.Empty;
    public int TotalScanned { get; set; }
    public int IdenticalCount { get; set; }
    public int ChangedCount { get; set; }
    public int MissingInSourceCount { get; set; }
    public int MissingInTargetCount { get; set; }
    public List<ObjectComparisonResult> Results { get; set; } = new();

    public IEnumerable<ObjectComparisonResult> Changed =>
        Results.Where(r => r.Status == DiffStatus.Changed);
    public IEnumerable<ObjectComparisonResult> MissingInSource =>
        Results.Where(r => r.Status == DiffStatus.MissingInSource);
    public IEnumerable<ObjectComparisonResult> MissingInTarget =>
        Results.Where(r => r.Status == DiffStatus.MissingInTarget);
    public IEnumerable<ObjectComparisonResult> Identical =>
        Results.Where(r => r.Status == DiffStatus.Identical);
}
