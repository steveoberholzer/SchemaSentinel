using SchemaSentinel.Core.Models;

namespace SchemaSentinel.UI.ViewModels;

public class ComparisonOptionsViewModel : ViewModelBase
{
    private ComparisonMode _mode = ComparisonMode.Logical;
    private bool _compareTables = true;
    private bool _compareViews = true;
    private bool _compareProcedures = true;
    private bool _compareFunctions = true;
    private bool _ignoreColumnOrder = true;
    private bool _sortColumnsAlphabetically = false;
    private bool _ignoreWhitespace = true;
    private bool _ignoreCasing = false;
    private bool _ignoreSetStatements = true;
    private DateTime? _changedSince;
    private bool _useChangedSince;

    public ComparisonMode Mode { get => _mode; set { SetField(ref _mode, value); OnPropertyChanged(nameof(IsLogical)); OnPropertyChanged(nameof(IsStrict)); } }
    public bool IsLogical { get => Mode == ComparisonMode.Logical; set { if (value) Mode = ComparisonMode.Logical; } }
    public bool IsStrict { get => Mode == ComparisonMode.Strict; set { if (value) Mode = ComparisonMode.Strict; } }

    public bool CompareTables { get => _compareTables; set => SetField(ref _compareTables, value); }
    public bool CompareViews { get => _compareViews; set => SetField(ref _compareViews, value); }
    public bool CompareProcedures { get => _compareProcedures; set => SetField(ref _compareProcedures, value); }
    public bool CompareFunctions { get => _compareFunctions; set => SetField(ref _compareFunctions, value); }
    public bool IgnoreColumnOrder { get => _ignoreColumnOrder; set => SetField(ref _ignoreColumnOrder, value); }
    public bool SortColumnsAlphabetically { get => _sortColumnsAlphabetically; set => SetField(ref _sortColumnsAlphabetically, value); }
    public bool IgnoreWhitespace { get => _ignoreWhitespace; set => SetField(ref _ignoreWhitespace, value); }
    public bool IgnoreCasing { get => _ignoreCasing; set => SetField(ref _ignoreCasing, value); }
    public bool IgnoreSetStatements { get => _ignoreSetStatements; set => SetField(ref _ignoreSetStatements, value); }
    public DateTime? ChangedSince { get => _changedSince; set => SetField(ref _changedSince, value); }
    public bool UseChangedSince { get => _useChangedSince; set { SetField(ref _useChangedSince, value); if (!value) ChangedSince = null; } }

    public ComparisonOptions ToOptions()
    {
        var opts = new ComparisonOptions
        {
            Mode = Mode,
            CompareTables = CompareTables,
            CompareViews = CompareViews,
            CompareProcedures = CompareProcedures,
            CompareFunctions = CompareFunctions,
            ChangedSince = UseChangedSince ? ChangedSince : null
        };

        if (Mode == ComparisonMode.Logical)
        {
            opts.IgnoreColumnOrder = IgnoreColumnOrder;
            opts.SortColumnsAlphabetically = SortColumnsAlphabetically;
            opts.IgnoreWhitespace = IgnoreWhitespace;
            opts.IgnoreCasing = IgnoreCasing;
            opts.IgnoreSetStatements = IgnoreSetStatements;
        }

        return opts;
    }
}
