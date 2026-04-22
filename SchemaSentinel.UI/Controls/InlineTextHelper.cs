using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using SchemaSentinel.Core.Comparison;

namespace SchemaSentinel.UI.Controls;

public static class InlineTextHelper
{
    private static readonly SolidColorBrush LeftHighlight  = new(Color.FromRgb(0xFF, 0x88, 0x88));
    private static readonly SolidColorBrush RightHighlight = new(Color.FromRgb(0x55, 0xBB, 0x55));

    public static readonly DependencyProperty LeftSegmentsProperty =
        DependencyProperty.RegisterAttached("LeftSegments",
            typeof(IReadOnlyList<TextSegment>), typeof(InlineTextHelper),
            new PropertyMetadata(null, (d, e) => Apply(d, e.NewValue as IReadOnlyList<TextSegment>, LeftHighlight)));

    public static readonly DependencyProperty RightSegmentsProperty =
        DependencyProperty.RegisterAttached("RightSegments",
            typeof(IReadOnlyList<TextSegment>), typeof(InlineTextHelper),
            new PropertyMetadata(null, (d, e) => Apply(d, e.NewValue as IReadOnlyList<TextSegment>, RightHighlight)));

    public static void SetLeftSegments(DependencyObject d, IReadOnlyList<TextSegment>? v)  => d.SetValue(LeftSegmentsProperty, v);
    public static IReadOnlyList<TextSegment>? GetLeftSegments(DependencyObject d)           => (IReadOnlyList<TextSegment>?)d.GetValue(LeftSegmentsProperty);
    public static void SetRightSegments(DependencyObject d, IReadOnlyList<TextSegment>? v) => d.SetValue(RightSegmentsProperty, v);
    public static IReadOnlyList<TextSegment>? GetRightSegments(DependencyObject d)          => (IReadOnlyList<TextSegment>?)d.GetValue(RightSegmentsProperty);

    private static void Apply(DependencyObject d, IReadOnlyList<TextSegment>? segments, SolidColorBrush highlight)
    {
        if (d is not TextBlock tb) return;
        tb.Inlines.Clear();
        if (segments == null) return;
        foreach (var seg in segments)
        {
            var run = new Run(seg.Text);
            if (seg.IsHighlighted) run.Background = highlight;
            tb.Inlines.Add(run);
        }
    }
}
