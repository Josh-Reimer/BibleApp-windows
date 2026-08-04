using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace BibleApp.Native.Controls;

/// <summary>
/// Renders a verse into a TextBlock as "1:1: In the beginning..." — the same shape the
/// original app's reading pane showed — with the reference dimmed and any search term
/// highlighted.
///
/// TextBlock.Inlines is not bindable, so the runs are built here from attached properties.
/// </summary>
public static class VerseTextBehavior
{
    public static readonly DependencyProperty ReferenceProperty =
        DependencyProperty.RegisterAttached(
            "Reference", typeof(string), typeof(VerseTextBehavior),
            new PropertyMetadata(string.Empty, OnChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(VerseTextBehavior),
            new PropertyMetadata(string.Empty, OnChanged));

    /// <summary>Term to highlight. Empty means no highlighting.</summary>
    public static readonly DependencyProperty HighlightProperty =
        DependencyProperty.RegisterAttached(
            "Highlight", typeof(string), typeof(VerseTextBehavior),
            new PropertyMetadata(string.Empty, OnChanged));

    public static void SetReference(DependencyObject o, string v) => o.SetValue(ReferenceProperty, v);
    public static string GetReference(DependencyObject o) => (string)o.GetValue(ReferenceProperty);

    public static void SetText(DependencyObject o, string v) => o.SetValue(TextProperty, v);
    public static string GetText(DependencyObject o) => (string)o.GetValue(TextProperty);

    public static void SetHighlight(DependencyObject o, string v) => o.SetValue(HighlightProperty, v);
    public static string GetHighlight(DependencyObject o) => (string)o.GetValue(HighlightProperty);

    private static void OnChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is not TextBlock block) return;

        var reference = GetReference(block);
        var text = GetText(block);
        var highlight = GetHighlight(block);

        block.Inlines.Clear();

        if (!string.IsNullOrEmpty(reference))
        {
            block.Inlines.Add(new Run(reference + " ")
            {
                Foreground = Brush(block, "VerseReferenceBrush", Brushes.DimGray),
            });
        }

        if (string.IsNullOrEmpty(highlight))
        {
            block.Inlines.Add(new Run(text));
            return;
        }

        var highlightBrush = Brush(block, "HighlightBrush", Brushes.Yellow);
        var cursor = 0;
        while (cursor < text.Length)
        {
            var match = text.IndexOf(highlight, cursor, StringComparison.OrdinalIgnoreCase);
            if (match < 0)
            {
                block.Inlines.Add(new Run(text[cursor..]));
                break;
            }

            if (match > cursor)
                block.Inlines.Add(new Run(text[cursor..match]));

            block.Inlines.Add(new Run(text.Substring(match, highlight.Length))
            {
                Background = highlightBrush,
                FontWeight = FontWeights.Bold,
            });

            cursor = match + highlight.Length;
        }
    }

    private static Brush Brush(FrameworkElement element, string key, Brush fallback) =>
        element.TryFindResource(key) as Brush ?? fallback;
}
