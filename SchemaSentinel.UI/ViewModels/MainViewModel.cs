using System.Collections.ObjectModel;
using System.Windows;
using SchemaSentinel.Core.Comparison;
using SchemaSentinel.Core.Models;
using SchemaSentinel.Data;
using SchemaSentinel.Reporting;
using SchemaSentinel.UI.Windows;

namespace SchemaSentinel.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly MetadataExtractor _extractor = new(new SqlConnectionService());
    private readonly SchemaComparer _comparer = new();

    private string _statusMessage = "Ready.";
    private bool _isBusy;
    private ObjectResultViewModel? _selectedResult;
    private string _filterText = string.Empty;
    private bool _hideMissingInSource;
    private bool _hideTargetOnlyColumns;

    private static readonly ConnectionProfileStore _profileStore = new();

    public ConnectionViewModel Source { get; } = new(_profileStore) { Label = "Source" };
    public ConnectionViewModel Target { get; } = new(_profileStore) { Label = "Target" };
    public ComparisonOptionsViewModel Options { get; } = new();

    public ObservableCollection<ObjectResultViewModel> Results { get; } = new();

    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }
    public bool IsBusy { get => _isBusy; set => SetField(ref _isBusy, value); }

    public ObjectResultViewModel? SelectedResult
    {
        get => _selectedResult;
        set => SetField(ref _selectedResult, value);
    }

    public string FilterText
    {
        get => _filterText;
        set { SetField(ref _filterText, value); ApplyFilter(); }
    }
    public bool HideMissingInSource
    {
        get => _hideMissingInSource;
        set { SetField(ref _hideMissingInSource, value); ApplyFilter(); }
    }
    public bool HideTargetOnlyColumns
    {
        get => _hideTargetOnlyColumns;
        set { SetField(ref _hideTargetOnlyColumns, value); ApplyFilter(); }
    }

    public Action<ObjectResultViewModel>? ScrollRequested { get; set; }

    private int _totalCount, _identicalCount, _changedCount, _missingSourceCount, _missingTargetCount;
    public int TotalCount { get => _totalCount; set => SetField(ref _totalCount, value); }
    public int IdenticalCount { get => _identicalCount; set => SetField(ref _identicalCount, value); }
    public int ChangedCount { get => _changedCount; set => SetField(ref _changedCount, value); }
    public int MissingSourceCount { get => _missingSourceCount; set => SetField(ref _missingSourceCount, value); }
    public int MissingTargetCount { get => _missingTargetCount; set => SetField(ref _missingTargetCount, value); }

    public AsyncRelayCommand CompareCommand { get; }
    public RelayCommand ExportHtmlCommand { get; }
    public RelayCommand ExportMarkdownCommand { get; }
    public RelayCommand ExportJsonCommand { get; }
    public RelayCommand ExportSqlCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ScrollToChangedCommand { get; }
    public RelayCommand ScrollToMissingSourceCommand { get; }
    public RelayCommand ScrollToMissingTargetCommand { get; }
    public RelayCommand ScrollToIdenticalCommand { get; }

    private CancellationTokenSource? _cts;

    public MainViewModel()
    {
        CompareCommand = new AsyncRelayCommand(RunComparisonAsync, () => !IsBusy);
        ExportHtmlCommand = new RelayCommand(() => ExportAsync(new HtmlReporter()), () => Results.Any());
        ExportMarkdownCommand = new RelayCommand(() => ExportAsync(new MarkdownReporter()), () => Results.Any());
        ExportJsonCommand = new RelayCommand(() => ExportAsync(new JsonReporter()), () => Results.Any());
        ExportSqlCommand = new RelayCommand(() => ExportAsync(new SqlScriptReporter()), () => Results.Any());
        CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsBusy);
        ScrollToChangedCommand = new RelayCommand(() => ScrollToFirstOf(DiffStatus.Changed));
        ScrollToMissingSourceCommand = new RelayCommand(() => ScrollToFirstOf(DiffStatus.MissingInSource));
        ScrollToMissingTargetCommand = new RelayCommand(() => ScrollToFirstOf(DiffStatus.MissingInTarget));
        ScrollToIdenticalCommand = new RelayCommand(() => ScrollToFirstOf(DiffStatus.Identical));
    }

    private async Task RunComparisonAsync(CancellationToken cancellationToken)
    {
        if (!Source.IsValid() || !Target.IsValid())
        {
            StatusMessage = "Please fill in both connection details before comparing.";
            return;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IsBusy = true;
        Results.Clear();
        SelectedResult = null;
        StatusMessage = "Extracting metadata from source...";

        try
        {
            var options = Options.ToOptions();
            var srcProfile = Source.ToProfile();
            var tgtProfile = Target.ToProfile();

            var sourceTables = options.CompareTables
                ? await _extractor.ExtractTablesAsync(srcProfile, options, _cts.Token)
                : Array.Empty<SchemaSentinel.Core.Models.TableModel>();

            StatusMessage = "Extracting metadata from target...";
            var targetTables = options.CompareTables
                ? await _extractor.ExtractTablesAsync(tgtProfile, options, _cts.Token)
                : Array.Empty<SchemaSentinel.Core.Models.TableModel>();

            StatusMessage = "Extracting modules from source...";
            var sourceModules = await _extractor.ExtractModulesAsync(srcProfile, options, _cts.Token);

            StatusMessage = "Extracting modules from target...";
            var targetModules = await _extractor.ExtractModulesAsync(tgtProfile, options, _cts.Token);

            StatusMessage = "Comparing...";
            var summary = await Task.Run(() =>
            {
                var result = _comparer.Compare(sourceTables, targetTables, sourceModules, targetModules, options);
                result.SourceDescription = srcProfile.DisplayName;
                result.TargetDescription = tgtProfile.DisplayName;
                return result;
            }, _cts.Token);

            _lastSummary = summary;
            _lastOptions = options;
            ApplyFilter();
            StatusMessage = $"Comparison complete. {summary.TotalScanned} objects scanned.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Comparison cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ComparisonSummary? _lastSummary;
    private ComparisonOptions? _lastOptions;

    private void ApplyFilter()
    {
        if (_lastSummary == null) return;

        Results.Clear();
        var vms = new List<ObjectResultViewModel>();

        foreach (var r in _lastSummary.Results.OrderBy(r => r.Status).ThenBy(r => r.ObjectType).ThenBy(r => r.FullName))
        {
            if (_hideMissingInSource && r.Status == DiffStatus.MissingInSource)
                continue;

            if (_lastOptions?.ChangedSince.HasValue == true)
            {
                var cutoff = _lastOptions.ChangedSince.Value;
                bool recentEnough = r.Status switch
                {
                    DiffStatus.MissingInTarget => r.SourceModifyDate >= cutoff,
                    DiffStatus.MissingInSource => r.TargetModifyDate >= cutoff,
                    _ => r.SourceModifyDate >= cutoff || r.TargetModifyDate >= cutoff
                };
                if (!recentEnough) continue;
            }

            ObjectComparisonResult effective = r;
            if (_hideTargetOnlyColumns && r.ObjectType == "Table" && r.Status == DiffStatus.Changed)
            {
                var filteredDiffs = r.DetailedDifferences
                    .Where(d => !d.StartsWith(SchemaComparer.TargetOnlyColumnMarker))
                    .ToList();
                if (filteredDiffs.Count == r.DetailedDifferences.Count)
                    effective = r;
                else
                {
                    effective = new ObjectComparisonResult
                    {
                        ObjectType = r.ObjectType, SchemaName = r.SchemaName, ObjectName = r.ObjectName,
                        Status = filteredDiffs.Any() ? DiffStatus.Changed : DiffStatus.Identical,
                        SummaryMessage = filteredDiffs.Any() ? $"{filteredDiffs.Count} difference(s) found." : "Identical.",
                        DetailedDifferences = filteredDiffs,
                        SourceNormalizedDefinition = r.SourceNormalizedDefinition,
                        TargetNormalizedDefinition = r.TargetNormalizedDefinition,
                        AlterScript = r.AlterScript
                    };
                    if (effective.Status == DiffStatus.Identical) continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                var f = FilterText.Trim();
                if (!effective.FullName.Contains(f, StringComparison.OrdinalIgnoreCase) &&
                    !effective.ObjectType.Contains(f, StringComparison.OrdinalIgnoreCase) &&
                    !effective.Status.ToString().Contains(f, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            vms.Add(new ObjectResultViewModel(effective));
        }

        foreach (var vm in vms)
            Results.Add(vm);

        TotalCount = vms.Count;
        IdenticalCount = vms.Count(v => v.Status == DiffStatus.Identical);
        ChangedCount = vms.Count(v => v.Status == DiffStatus.Changed);
        MissingSourceCount = vms.Count(v => v.Status == DiffStatus.MissingInSource);
        MissingTargetCount = vms.Count(v => v.Status == DiffStatus.MissingInTarget);
    }

    private void ScrollToFirstOf(DiffStatus status)
    {
        var item = Results.FirstOrDefault(r => r.Status == status);
        if (item == null) return;
        SelectedResult = item;
        ScrollRequested?.Invoke(item);
    }

    private async void ExportAsync(IReportExporter exporter)
    {
        if (_lastSummary == null) return;

        try
        {
            StatusMessage = "Generating preview...";
            var content = await exporter.GenerateAsync(_lastSummary);
            StatusMessage = "Ready.";

            var preview = new ExportPreviewWindow(exporter.DisplayName, exporter.FileExtension, content)
            {
                Owner = Application.Current.MainWindow
            };
            preview.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Export failed.";
        }
    }
}

