using System.Windows;
using System.Windows.Controls;
using SchemaSentinel.Core.Comparison;

namespace SchemaSentinel.UI.Controls;

public class DiffLineTemplateSelector : DataTemplateSelector
{
    public DataTemplate? PlainTemplate    { get; set; }
    public DataTemplate? ModifiedTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        item is DiffLine { Kind: DiffKind.Modified } ? ModifiedTemplate : PlainTemplate;
}
