using System.Windows;
using SchemaSentinel.UI.ViewModels;

namespace SchemaSentinel.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.ScrollRequested = item =>
                {
                    ResultsGrid.SelectedItem = item;
                    ResultsGrid.ScrollIntoView(item);
                };
        };
    }
}
