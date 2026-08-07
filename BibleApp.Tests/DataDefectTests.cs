namespace BibleApp.Tests;

/// <summary>
/// The source .txt files carry four known defects. Three are compensated for in the
/// parser; the fourth is a genuine hole in the data and is asserted here so that it stays
/// documented rather than being rediscovered.
///
/// If one of these starts failing, check whether the data files changed before assuming
/// the parser broke.
/// </summary>
[Collection(BibleCollection.Name)]
public sealed class DataDefectTests(BibleFixture fixture)
{
    /// <summary>
    /// galatians.txt line 146 lost a newline, so 6:15 and 6:16 share a line. The Python
    /// version reads the line as a single verse and silently drops 6:16.
    /// </summary>
    [Fact]
    public void Galatians_6_16_is_recovered_from_the_run_together_line()
    {
        var verse = fixture.Find("galatians.txt", 6, 16);

        Assert.NotNull(verse);
        Assert.StartsWith("And as many as walk according to this rule", verse.Text);
        Assert.EndsWith("upon the Israel of God.", verse.Text);
    }

    [Fact]
    public void Galatians_6_15_does_not_absorb_the_verse_that_follows_it()
    {
        var verse = fixture.Find("galatians.txt", 6, 15);

        Assert.NotNull(verse);
        Assert.EndsWith("but a new creature.", verse.Text);
        Assert.DoesNotContain("6:16", verse.Text);
    }

    /// <summary>
    /// Only one line in the whole corpus runs two verses together. If this count rises,
    /// the data changed — and the splitting rule deserves another look, because it is
    /// deliberately conservative.
    /// </summary>
    [Fact]
    public void Galatians_6_is_the_only_chapter_needing_a_split()
    {
        var galatians = fixture.Book("galatians.txt");
        Assert.Equal(18, galatians.Chapter(6).Count);
    }

    /// <summary>
    /// first_thesselonians.txt stores 1:1 as " 1:1: Paul, and Silvanus..." with a leading
    /// space. Python's int() skips leading whitespace, so the original coped by accident;
    /// a regex anchored hard to ^ would drop the verse.
    /// </summary>
    [Fact]
    public void First_Thessalonians_1_1_survives_its_leading_space()
    {
        var verse = fixture.Find("first_thesselonians.txt", 1, 1);

        Assert.NotNull(verse);
        Assert.StartsWith("Paul, and Silvanus, and Timotheus", verse.Text);
    }

    /// <summary>
    /// isaiah.txt carries "ISAIAH Chapters 34-66" at line 651, between verses rather than
    /// at the top of the file, so heading lines cannot be assumed to come first.
    /// </summary>
    [Fact]
    public void Isaiah_mid_file_divider_is_preserved()
    {
        var isaiah = fixture.Book("isaiah.txt");

        var dividerIndex = isaiah.Content
            .ToList()
            .FindIndex(l => l.Heading is not null && l.Heading.Contains("Chapters 34-66"));

        Assert.True(dividerIndex > 0, "divider was dropped");

        // It must sit after real verses, which is the case the parser has to handle.
        Assert.Contains(isaiah.Content.Take(dividerIndex), l => l.Verse is not null);
    }

    /// <summary>
    /// second_corinthians.txt begins at "1:2:". The verse is not in the file — this is a
    /// data problem, not a parsing one, and fixing it means writing scripture text into
    /// the repository. Asserted so the gap stays visible.
    /// </summary>
    [Fact]
    public void Second_Corinthians_1_1_is_absent_from_the_source_data()
    {
        Assert.Null(fixture.Find("second_corinthians.txt", 1, 1));
    }

    [Fact]
    public void Second_Corinthians_chapter_1_therefore_starts_at_verse_2()
    {
        var first = fixture.Book("second_corinthians.txt").Chapter(1).First();

        Assert.Equal(2, first.Number);
        Assert.StartsWith("Grace be to you and peace", first.Text);
    }

    /// <summary>
    /// Every chapter except the one above should number its verses 1..n with no gaps.
    /// </summary>
    [Fact]
    public void No_other_chapter_is_missing_a_verse()
    {
        var gaps = new List<string>();

        foreach (var book in fixture.Bible.Books)
        {
            for (var chapter = 1; chapter <= book.ChapterCount; chapter++)
            {
                var numbers = book.Chapter(chapter).Select(v => v.Number).ToList();
                if (!numbers.SequenceEqual(Enumerable.Range(1, numbers.Count)))
                    gaps.Add($"{book.DisplayName} {chapter}");
            }
        }

        Assert.Equal(["Second Corinthians 1"], gaps);
    }
}
