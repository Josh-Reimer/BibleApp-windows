using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BibleApp.Native.Models;

/// <summary>One row of a book file: either a heading line or a verse.</summary>
public readonly record struct BookLine(string? Heading, Verse? Verse);

/// <summary>
/// One book of the Bible, parsed from a single .txt file.
/// </summary>
public sealed partial class BibleBook
{
    /// <summary>Original data file name, e.g. "first_samuel.txt". Kept because the
    /// search-range feature and the on-disk layout are both keyed off it.</summary>
    public string FileName { get; }

    /// <summary>Name for display, e.g. "First Samuel".</summary>
    public string DisplayName { get; }

    /// <summary>Every line in file order, headings included. Isaiah, for one, carries a
    /// "ISAIAH Chapters 34-66" divider partway through, so headings are not only
    /// found at the top of a file.</summary>
    public IReadOnlyList<BookLine> Content => _content;

    public IReadOnlyList<Verse> Verses => _verses;

    private readonly List<BookLine> _content = [];
    private readonly List<Verse> _verses = [];
    private Dictionary<int, List<Verse>>? _byChapter;

    private BibleBook(string fileName, string displayName)
    {
        FileName = fileName;
        DisplayName = displayName;
    }

    /// <summary>
    /// Matches a verse marker: at the start of a line, or embedded mid-line after a
    /// non-digit. See <see cref="ParseLine"/> for why the embedded case exists.
    ///
    /// The leading marker tolerates indentation — 1 Thessalonians 1:1 is stored as
    /// " 1:1: Paul, and Silvanus..." with a leading space, and anchoring hard to the
    /// start of the line would drop the verse.
    /// </summary>
    [GeneratedRegex(@"(?:^[ \t]*|(?<=\D))(\d+):(\d+): ?")]
    private static partial Regex VerseMarker();

    private Dictionary<int, List<Verse>> ByChapter
    {
        get
        {
            if (_byChapter is null)
            {
                _byChapter = new Dictionary<int, List<Verse>>();
                foreach (var verse in _verses)
                {
                    if (!_byChapter.TryGetValue(verse.Chapter, out var list))
                        _byChapter[verse.Chapter] = list = new List<Verse>();
                    list.Add(verse);
                }
            }
            return _byChapter;
        }
    }

    public int ChapterCount => ByChapter.Count == 0 ? 0 : ByChapter.Keys.Max();

    public IReadOnlyList<Verse> Chapter(int chapter) =>
        ByChapter.TryGetValue(chapter, out var list) ? list : [];

    /// <summary>
    /// Reads and parses one book file.
    /// </summary>
    /// <param name="displayName">Overrides the name derived from the file name.
    /// Used to correct misspelled data files without renaming them on disk.</param>
    public static BibleBook Parse(string path, string? displayName = null)
    {
        var fileName = Path.GetFileName(path);
        var book = new BibleBook(fileName, displayName ?? ToDisplayName(fileName));

        foreach (var line in File.ReadLines(path))
            book.ParseLine(line);

        return book;
    }

    /// <summary>
    /// A line is normally one verse. Two verses can also share a line where the source
    /// text lost a newline — galatians.txt line 146 runs 6:15 and 6:16 together, and the
    /// Python version silently swallowed 6:16 as a result. An embedded marker is only
    /// honoured when it continues the line's own chapter and verse numbering, so ordinary
    /// text that happens to look like "3:16: " is left alone.
    /// </summary>
    private void ParseLine(string line)
    {
        var markers = VerseMarker().Matches(line);

        if (markers.Count == 0 || markers[0].Index != 0)
        {
            _content.Add(new BookLine(line, null));
            return;
        }

        var chapter = int.Parse(markers[0].Groups[1].ValueSpan);
        var previousVerse = int.Parse(markers[0].Groups[2].ValueSpan);

        // Keep the leading marker plus any embedded marker that continues the sequence.
        var accepted = new List<Match> { markers[0] };
        for (var i = 1; i < markers.Count; i++)
        {
            var nextChapter = int.Parse(markers[i].Groups[1].ValueSpan);
            var nextVerse = int.Parse(markers[i].Groups[2].ValueSpan);
            if (nextChapter != chapter || nextVerse != previousVerse + 1) continue;

            accepted.Add(markers[i]);
            previousVerse = nextVerse;
        }

        for (var i = 0; i < accepted.Count; i++)
        {
            var start = accepted[i].Index + accepted[i].Length;
            var end = i + 1 < accepted.Count ? accepted[i + 1].Index : line.Length;

            var verse = new Verse
            {
                Book = this,
                Chapter = int.Parse(accepted[i].Groups[1].ValueSpan),
                Number = int.Parse(accepted[i].Groups[2].ValueSpan),
                Text = line[start..end].Trim(),
            };

            _verses.Add(verse);
            _content.Add(new BookLine(null, verse));
        }
    }

    /// <summary>"first_samuel.txt" -> "First Samuel".</summary>
    public static string ToDisplayName(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName).Replace('_', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(stem);
    }
}
