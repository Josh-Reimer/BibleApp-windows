using System.Text.Json;
using BibleApp.Native.Models;

namespace BibleApp.Tests;

/// <summary>
/// red_letter_kjv.json comes from the Bible-Verse Android app. Each entry carries its own
/// copy of the verse, and <see cref="RedLetter.Apply"/> skips any entry whose copy differs
/// from the parsed verse — so a drift in the data shows up here as a missing verse rather
/// than as the wrong words in red.
/// </summary>
[Collection(BibleCollection.Name)]
public sealed class RedLetterTests(BibleFixture fixture)
{
    [Fact]
    public void Every_entry_in_the_file_lands_on_a_verse()
    {
        var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FindDataFile()))!;

        var marked = fixture.Bible.AllVerses.Count(v => v.WordsOfChrist.Count > 0);

        Assert.Equal(2027, entries.Count);
        Assert.Equal(entries.Count, marked);
    }

    [Fact]
    public void Only_the_spoken_words_are_red_when_narration_shares_the_verse()
    {
        var verse = fixture.Find("matthew.txt", 3, 15)!;

        var span = Assert.Single(verse.WordsOfChrist);
        Assert.StartsWith("And Jesus answering said unto him, ", verse.Text[..span.Start]);
        Assert.StartsWith("Suffer it to be so now", verse.Text[span.Start..]);
        Assert.Equal(" Then he suffered him.", verse.Text[span.End..]);
    }

    [Fact]
    public void A_verse_spoken_entirely_by_Christ_is_red_throughout()
    {
        var verse = fixture.Find("matthew.txt", 5, 3)!;

        var span = Assert.Single(verse.WordsOfChrist);
        Assert.Equal(new TextSpan(0, verse.Text.Length), span);
    }

    [Fact]
    public void The_Old_Testament_has_no_red_letters()
    {
        var oldTestament = fixture.Bible.Books.TakeWhile(b => b.FileName != "matthew.txt");

        Assert.All(oldTestament.SelectMany(b => b.Verses), v => Assert.Empty(v.WordsOfChrist));
    }

    [Fact]
    public void Parse_splits_markup_into_text_and_spans()
    {
        var (text, spans) = RedLetter.Parse(
            "He said, <font color=\"#CC0000\">Come.</font> And <font color=\"#CC0000\">Go.</font>");

        Assert.Equal("He said, Come. And Go.", text);
        Assert.Equal([new TextSpan(9, 5), new TextSpan(19, 3)], spans);
    }

    [Fact]
    public void A_missing_file_leaves_the_Bible_without_red_letters()
    {
        Assert.Equal(0, RedLetter.Apply(fixture.Bible.Books, Path.Combine(Path.GetTempPath(), "no-such-file.json")));
    }

    /// <summary>Same search <see cref="BibleLibrary"/> does: the repo root sits a few levels up.</summary>
    private static string FindDataFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, RedLetter.FileName)))
            dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? AppContext.BaseDirectory, RedLetter.FileName);
    }
}
