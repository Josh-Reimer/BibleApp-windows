using System.Text.RegularExpressions;
using BibleApp.Native.Models;

namespace BibleApp.Tests;

[Collection(BibleCollection.Name)]
public sealed class BibleLibraryTests(BibleFixture fixture)
{
    private readonly BibleLibrary _bible = fixture.Bible;

    [Fact]
    public void Loads_all_66_books()
    {
        Assert.Equal(66, _bible.Books.Count);
    }

    [Fact]
    public void Books_are_in_canonical_order()
    {
        Assert.Equal(BibleLibrary.CanonicalFiles, _bible.Books.Select(b => b.FileName));
    }

    /// <summary>
    /// A standard KJV has 31,102. The shortfall is documented in <see cref="DataDefectTests"/>:
    /// 2 Corinthians 1:1 is absent from the source data, and the remainder is most likely
    /// verses missing from the end of a chapter, which a gap check cannot detect without a
    /// reference text. This number is pinned so that a change to the data or the parser
    /// has to be deliberate.
    /// </summary>
    [Fact]
    public void Parses_31098_verses()
    {
        Assert.Equal(31_098, _bible.AllVerses.Count);
    }

    [Theory]
    [InlineData("genesis.txt", 50)]
    [InlineData("psalms.txt", 150)]
    [InlineData("isaiah.txt", 66)]
    [InlineData("matthew.txt", 28)]
    [InlineData("revelation.txt", 22)]
    [InlineData("obadiah.txt", 1)]
    [InlineData("jude.txt", 1)]
    public void Book_has_expected_chapter_count(string file, int chapters)
    {
        Assert.Equal(chapters, fixture.Book(file).ChapterCount);
    }

    [Theory]
    [InlineData("genesis.txt", 1, 31)]
    [InlineData("psalms.txt", 119, 176)]
    [InlineData("psalms.txt", 117, 2)]
    [InlineData("john.txt", 3, 36)]
    [InlineData("revelation.txt", 22, 21)]
    public void Chapter_has_expected_verse_count(string file, int chapter, int verses)
    {
        Assert.Equal(verses, fixture.Book(file).Chapter(chapter).Count);
    }

    /// <summary>
    /// Several data files are misspelled and must keep those names on disk, because
    /// BibleFileNames.py refers to them that way. Only the display name is corrected.
    /// </summary>
    [Theory]
    [InlineData("eccliasiastes.txt", "Ecclesiastes")]
    [InlineData("ezekial.txt", "Ezekiel")]
    [InlineData("philipians.txt", "Philippians")]
    [InlineData("first_thesselonians.txt", "First Thessalonians")]
    [InlineData("second_thesselonians.txt", "Second Thessalonians")]
    [InlineData("first_samuel.txt", "First Samuel")]
    [InlineData("song_of_solomon.txt", "Song Of Solomon")]
    public void Book_displays_under_corrected_name(string file, string expected)
    {
        Assert.Equal(expected, fixture.Book(file).DisplayName);
    }

    [Fact]
    public void Verse_reference_uses_the_corrected_book_name()
    {
        Assert.Equal("Ezekiel 1:1", fixture.Find("ezekial.txt", 1, 1)?.Reference);
    }

    [Fact]
    public void Verse_text_never_retains_its_own_marker()
    {
        var offenders = _bible.AllVerses
            .Where(v => Regex.IsMatch(v.Text, @"^\d+:\d+:"))
            .Take(5)
            .Select(v => v.Reference)
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void No_verse_is_empty()
    {
        var empty = _bible.AllVerses
            .Where(v => v.Text.Length == 0)
            .Take(5)
            .Select(v => v.Reference)
            .ToList();

        Assert.Empty(empty);
    }

    [Fact]
    public void Verse_text_is_intact()
    {
        Assert.StartsWith(
            "For God so loved the world",
            fixture.Find("john.txt", 3, 16)?.Text);
    }

    [Fact]
    public void Chapter_numbering_has_no_holes()
    {
        foreach (var book in _bible.Books)
        {
            var chapters = book.Verses.Select(v => v.Chapter).Distinct().Order().ToList();
            Assert.Equal(Enumerable.Range(1, book.ChapterCount), chapters);
        }
    }

    [Fact]
    public void Book_content_preserves_every_verse_in_file_order()
    {
        var book = fixture.Book("genesis.txt");
        var fromContent = book.Content.Where(l => l.Verse is not null).Select(l => l.Verse!);

        Assert.Equal(book.Verses, fromContent);
    }
}
