using System.ComponentModel;
using System.Windows.Controls;
using SchemaSentinel.UI.ViewModels;

namespace SchemaSentinel.UI.Controls;

public partial class ConnectionPanel : UserControl
{
    public ConnectionPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ConnectionViewModel old)
            old.PropertyChanged -= OnVmPropertyChanged;

        if (e.NewValue is ConnectionViewModel vm)
        {
            vm.PropertyChanged += OnVmPropertyChanged;
            if (PasswordBox != null)
                PasswordBox.Password = vm.Password;
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConnectionViewModel.Password) && sender is ConnectionViewModel vm)
            PasswordBox.Password = vm.Password;
    }

    private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ConnectionViewModel vm)
            vm.Password = PasswordBox.Password;
    }
}
