using System.Collections.Generic;

namespace BibleApp.Native.Models;

/// <summary>
/// One row in the reading pane. Either a heading line lifted from the top of a book
/// file (<see cref="Verse"/> null) or a verse.
/// </summary>
public sealed class DisplayLine
{
    /// <summary>Prefix shown dimmed before the text: "1:1:" inside a book, or the full
    /// "Genesis 1:1" in search results. Empty for heading lines.</summary>
    public string Reference { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    /// <summary>Search term to highlight within <see cref="Text"/>. Empty for none.</summary>
    public string Highlight { get; init; } = string.Empty;

    public Verse? Verse { get; init; }

    /// <summary>Red-letter spans within <see cref="Text"/>; empty for heading lines.</summary>
    public IReadOnlyList<TextSpan> WordsOfChrist => Verse?.WordsOfChrist ?? [];

    public static DisplayLine Heading(string text) => new() { Text = text };

    /// <summary>Renders a book's own line — heading or verse — as it sits in the file.</summary>
    public static DisplayLine FromBookLine(BookLine line) =>
        line.Verse is null ? Heading(line.Heading ?? string.Empty) : InBook(line.Verse);

    /// <summary>Verse as it appears inside a book or chapter: "1:1: In the beginning...".</summary>
    public static DisplayLine InBook(Verse verse, string highlight = "") => new()
    {
        Reference = $"{verse.Chapter}:{verse.Number}:",
        Text = verse.Text,
        Highlight = highlight,
        Verse = verse,
    };

    /// <summary>Verse as it appears in search results, qualified by book name.</summary>
    public static DisplayLine InResults(Verse verse, string highlight) => new()
    {
        Reference = verse.Reference,
        Text = verse.Text,
        Highlight = highlight,
        Verse = verse,
    };
}
