using SchemaSentinel.Core.Comparison;

namespace SchemaSentinel.UI.ViewModels;

public class ObjectResultViewModel : ViewModelBase
{
    private bool _isSelected;

    public ObjectResultViewModel(ObjectComparisonResult result)
    {
        Result = result;
    }

    public ObjectComparisonResult Result { get; }

    public string ObjectType => Result.ObjectType;
    public string SchemaName => Result.SchemaName;
    public string ObjectName => Result.ObjectName;
    public string FullName => Result.FullName;
    public DiffStatus Status => Result.Status;
    public string StatusText => Result.Status switch
    {
        DiffStatus.Identical => "Identical",
        DiffStatus.Changed => "Changed",
        DiffStatus.MissingInSource => "Missing in Source",
        DiffStatus.MissingInTarget => "Missing in Target",
        _ => Result.Status.ToString()
    };
    public string SummaryMessage => Result.SummaryMessage;
    public List<string> DetailedDifferences => Result.DetailedDifferences;
    public string DifferencesText => Result.DetailedDifferences.Any()
        ? string.Join(Environment.NewLine, Result.DetailedDifferences)
        : Result.SummaryMessage;
    public string? SourceDefinition => Result.SourceNormalizedDefinition;
    public string? TargetDefinition => Result.TargetNormalizedDefinition;
    public bool HasDefinitions => SourceDefinition != null || TargetDefinition != null;

    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }
}
