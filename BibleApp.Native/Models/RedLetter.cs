using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BibleApp.Native.Models;

/// <summary>A run of characters within a verse's <see cref="Verse.Text"/>.</summary>
public readonly record struct TextSpan(int Start, int Length)
{
    public int End => Start + Length;

    public bool Contains(int index) => index >= Start && index < End;
}

/// <summary>
/// Red-letter (words of Christ) markup, shared with the Bible-Verse Android app.
///
/// red_letter_kjv.json maps "bookIndex:chapter:verse" — bookIndex into
/// <see cref="BibleLibrary.CanonicalFiles"/> — to the verse's full text with the words of
/// Christ wrapped in &lt;font color="#CC0000"&gt;…&lt;/font&gt;. It was parsed from the
/// CrossWire KJV OSIS &lt;q who="Jesus"&gt; spans and corrected to match this text word for
/// word; 2,027 verses carry markup.
///
/// No WPF here, so the tests can reach it — see BibleApp.Tests.csproj.
/// </summary>
public static partial class RedLetter
{
    public const string FileName = "red_letter_kjv.json";

    [GeneratedRegex("<font[^>]*>(.*?)</font>", RegexOptions.Singleline)]
    private static partial Regex FontSpan();

    /// <summary>
    /// Fills in <see cref="Verse.WordsOfChrist"/> for every verse the file marks up.
    /// A missing file leaves the Bible without red letters rather than failing to load.
    /// </summary>
    /// <returns>How many verses were marked.</returns>
    public static int Apply(IReadOnlyList<BibleBook> books, string path)
    {
        if (!File.Exists(path)) return 0;

        Dictionary<string, string>? markup;
        try
        {
            markup = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            return 0;
        }
        if (markup is null) return 0;

        var byFile = new Dictionary<string, BibleBook>(StringComparer.OrdinalIgnoreCase);
        foreach (var book in books) byFile[book.FileName] = book;

        var applied = 0;
        foreach (var (key, html) in markup)
        {
            var parts = key.Split(':');
            if (parts.Length != 3
                || !int.TryParse(parts[0], out var bookIndex)
                || !int.TryParse(parts[1], out var chapter)
                || !int.TryParse(parts[2], out var number)
                || bookIndex < 0 || bookIndex >= BibleLibrary.CanonicalFiles.Length
                || !byFile.TryGetValue(BibleLibrary.CanonicalFiles[bookIndex], out var book))
                continue;

            Verse? verse = null;
            foreach (var v in book.Chapter(chapter))
                if (v.Number == number) { verse = v; break; }
            if (verse is null) continue;

            // The markup carries its own copy of the verse. Only trust its offsets when that
            // copy is this verse exactly — otherwise the wrong words would turn red.
            var (text, spans) = Parse(html);
            if (text != verse.Text) continue;

            verse.WordsOfChrist = spans;
            applied++;
        }
        return applied;
    }

    /// <summary>Splits one entry's markup into its plain text and the red spans within it.</summary>
    public static (string Text, IReadOnlyList<TextSpan> Spans) Parse(string html)
    {
        var text = new System.Text.StringBuilder(html.Length);
        var spans = new List<TextSpan>();
        var cursor = 0;

        foreach (Match match in FontSpan().Matches(html))
        {
            text.Append(html, cursor, match.Index - cursor);
            var inner = match.Groups[1].Value;
            spans.Add(new TextSpan(text.Length, inner.Length));
            text.Append(inner);
            cursor = match.Index + match.Length;
        }
        text.Append(html, cursor, html.Length - cursor);

        // Verse.Text is trimmed, so trim here too and shift the spans to match.
        var raw = text.ToString();
        var trimmed = raw.Trim();
        var lead = raw.Length - raw.TrimStart().Length;
        var shifted = new List<TextSpan>(spans.Count);
        foreach (var span in spans)
        {
            var start = Math.Max(span.Start - lead, 0);
            var end = Math.Min(span.End - lead, trimmed.Length);
            if (end > start) shifted.Add(new TextSpan(start, end - start));
        }
        return (trimmed, shifted);
    }
}
