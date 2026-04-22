using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace SchemaSentinel.UI.Windows;

public partial class ExportPreviewWindow : Window
{
    private readonly string _displayName;
    private readonly string _extension;

    public ExportPreviewWindow(string displayName, string extension, string content)
    {
        InitializeComponent();
        _displayName = displayName;
        _extension = extension;
        Title = $"Preview — {displayName}";
        ContentBox.Text = content;
    }

    private void CopyAll_Click(object sender, RoutedEventArgs e)
        => Clipboard.SetText(ContentBox.Text);

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = $"{_displayName}|*{_extension}",
            FileName = $"SchemaSentinel-Report-{DateTime.Now:yyyyMMdd-HHmmss}{_extension}"
        };
        if (dialog.ShowDialog(this) != true) return;
        File.WriteAllText(dialog.FileName, ContentBox.Text, Encoding.UTF8);
        MessageBox.Show(this, $"Saved to:\n{dialog.FileName}", "Saved",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
