namespace SchemaSentinel.Core.Models;

public enum ComparisonMode { Strict, Logical }

public class ComparisonOptions
{
    public ComparisonMode Mode { get; set; } = ComparisonMode.Logical;
    public bool CompareTables { get; set; } = true;
    public bool CompareViews { get; set; } = true;
    public bool CompareProcedures { get; set; } = true;
    public bool CompareFunctions { get; set; } = true;
    public bool IgnoreColumnOrder { get; set; } = true;
    public bool SortColumnsAlphabetically { get; set; } = false;
    public bool IgnoreWhitespace { get; set; } = true;
    public bool IgnoreCasing { get; set; } = false;
    public bool IgnoreSetStatements { get; set; } = true;
    public bool IgnoreComments { get; set; } = false;
    public bool NormalizeBrackets { get; set; } = false;
    public DateTime? ChangedSince { get; set; }
    public List<string> SchemaFilter { get; set; } = new();
}
