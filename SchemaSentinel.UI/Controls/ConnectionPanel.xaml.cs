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
        if (PasswordBox != null)
            PasswordBox.Password = string.Empty;
    }

    private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ConnectionViewModel vm)
            vm.Password = PasswordBox.Password;
    }
}
