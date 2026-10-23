using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BibleApp.Native.Models;

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

    /// <summary>Spans of the text that are the words of Christ, drawn in red.</summary>
    public static readonly DependencyProperty WordsOfChristProperty =
        DependencyProperty.RegisterAttached(
            "WordsOfChrist", typeof(IReadOnlyList<TextSpan>), typeof(VerseTextBehavior),
            new PropertyMetadata(null, OnChanged));

    public static void SetReference(DependencyObject o, string v) => o.SetValue(ReferenceProperty, v);
    public static string GetReference(DependencyObject o) => (string)o.GetValue(ReferenceProperty);

    public static void SetText(DependencyObject o, string v) => o.SetValue(TextProperty, v);
    public static string GetText(DependencyObject o) => (string)o.GetValue(TextProperty);

    public static void SetHighlight(DependencyObject o, string v) => o.SetValue(HighlightProperty, v);
    public static string GetHighlight(DependencyObject o) => (string)o.GetValue(HighlightProperty);

    public static void SetWordsOfChrist(DependencyObject o, IReadOnlyList<TextSpan>? v) => o.SetValue(WordsOfChristProperty, v);
    public static IReadOnlyList<TextSpan>? GetWordsOfChrist(DependencyObject o) => (IReadOnlyList<TextSpan>?)o.GetValue(WordsOfChristProperty);

    private static void OnChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is not TextBlock block) return;

        var reference = GetReference(block);
        var text = GetText(block);
        var highlight = GetHighlight(block);
        var red = GetWordsOfChrist(block) ?? [];

        block.Inlines.Clear();

        if (!string.IsNullOrEmpty(reference))
        {
            block.Inlines.Add(new Run(reference + " ")
            {
                Foreground = Brush(block, "VerseReferenceBrush", Brushes.DimGray),
            });
        }

        var highlights = FindAll(text, highlight);
        if (highlights.Count == 0 && red.Count == 0)
        {
            block.Inlines.Add(new Run(text));
            return;
        }

        // Red letters and search highlights overlap freely, so cut the text wherever
        // either one starts or stops and style each piece by what covers it.
        var cuts = new SortedSet<int> { 0, text.Length };
        foreach (var span in red) { cuts.Add(Math.Min(span.Start, text.Length)); cuts.Add(Math.Min(span.End, text.Length)); }
        foreach (var span in highlights) { cuts.Add(span.Start); cuts.Add(span.End); }

        var redBrush = Brush(block, "WordsOfChristBrush", RedLetterFallback);
        var highlightBrush = Brush(block, "HighlightBrush", Brushes.Yellow);
        var start = -1;
        foreach (var end in cuts)
        {
            if (start >= 0 && end > start)
            {
                var run = new Run(text[start..end]);
                if (red.Any(s => s.Contains(start))) run.Foreground = redBrush;
                if (highlights.Any(s => s.Contains(start)))
                {
                    run.Background = highlightBrush;
                    run.FontWeight = FontWeights.Bold;
                }
                block.Inlines.Add(run);
            }
            start = end;
        }
    }

    /// <summary>The red Bible-Verse uses, for a theme that does not define its own.</summary>
    private static readonly Brush RedLetterFallback = new SolidColorBrush(Color.FromRgb(0xCC, 0x00, 0x00));

    private static List<TextSpan> FindAll(string text, string term)
    {
        var found = new List<TextSpan>();
        if (string.IsNullOrEmpty(term)) return found;

        var cursor = 0;
        while (cursor < text.Length)
        {
            var match = text.IndexOf(term, cursor, StringComparison.OrdinalIgnoreCase);
            if (match < 0) break;
            found.Add(new TextSpan(match, term.Length));
            cursor = match + term.Length;
        }
        return found;
    }

    private static Brush Brush(FrameworkElement element, string key, Brush fallback) =>
        element.TryFindResource(key) as Brush ?? fallback;
}
