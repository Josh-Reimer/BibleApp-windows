using BibleApp.Native.Models;
using BibleApp.Native.Services;

namespace BibleApp.Tests;

[Collection(BibleCollection.Name)]
public sealed class SearchServiceTests(BibleFixture fixture)
{
    private IReadOnlyList<BibleBook> AllBooks => fixture.Bible.Books;

    [Fact]
    public void Finds_every_verse_containing_the_term()
    {
        var result = SearchService.Search(AllBooks, "In the beginning");

        Assert.Contains(result.Hits, h => h.Verse.Reference == "Genesis 1:1");
        Assert.Contains(result.Hits, h => h.Verse.Reference == "John 1:1");
        Assert.All(result.Hits, h =>
            Assert.Contains("in the beginning", h.Verse.Text, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Is_case_insensitive()
    {
        var upper = SearchService.Search(AllBooks, "LOVINGKINDNESS");
        var lower = SearchService.Search(AllBooks, "lovingkindness");

        Assert.Equal(upper.TotalMatches, lower.TotalMatches);
        Assert.Equal(29, lower.TotalMatches);
    }

    /// <summary>
    /// The Python version searched the raw line, so a term like "1:1" matched every verse
    /// whose reference prefix contained it. Searching the verse text only avoids that.
    /// </summary>
    [Fact]
    public void Does_not_match_reference_prefixes()
    {
        Assert.Equal(0, SearchService.Search(AllBooks, "1:1").TotalMatches);
    }

    [Fact]
    public void Match_offset_points_at_the_term_within_the_verse()
    {
        var result = SearchService.Search(AllBooks, "Jesus wept");
        var hit = Assert.Single(result.Hits);

        Assert.Equal("John 11:35", hit.Verse.Reference);
        Assert.Equal(
            "Jesus wept",
            hit.Verse.Text.Substring(hit.MatchStart, hit.MatchLength));
    }

    [Fact]
    public void Range_restricts_the_search_to_the_given_books()
    {
        var pentateuch = AllBooks.Take(5).ToList();
        var result = SearchService.Search(pentateuch, "God");

        Assert.True(result.TotalMatches > 0);
        Assert.All(result.Hits, h => Assert.Contains(h.Verse.Book, pentateuch));
    }

    [Fact]
    public void Range_of_one_book_stays_within_that_book()
    {
        var jude = new[] { fixture.Book("jude.txt") };
        var result = SearchService.Search(jude, "the");

        Assert.All(result.Hits, h => Assert.Equal("Jude", h.Verse.Book.DisplayName));
    }

    [Fact]
    public void Very_broad_search_truncates_but_still_reports_the_true_total()
    {
        var result = SearchService.Search(AllBooks, "e");

        Assert.True(result.Truncated);
        Assert.Equal(SearchService.MaxResults, result.Hits.Count);
        Assert.True(result.TotalMatches > result.Hits.Count);
    }

    [Fact]
    public void Empty_term_returns_nothing()
    {
        var result = SearchService.Search(AllBooks, "");

        Assert.Empty(result.Hits);
        Assert.Equal(0, result.TotalMatches);
        Assert.False(result.Truncated);
    }

    [Fact]
    public void Term_that_appears_nowhere_returns_nothing()
    {
        var result = SearchService.Search(AllBooks, "telephone");

        Assert.Empty(result.Hits);
        Assert.Equal(0, result.TotalMatches);
    }

    [Fact]
    public async Task Cancellation_stops_the_search()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => SearchService.SearchAsync(AllBooks, "God", cancellation.Token));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData("   ", true)]
    [InlineData("a", true)]
    [InlineData("of", true)]
    [InlineData("God", false)]
    [InlineData("lovingkindness", false)]
    public void Broadness_warning_fires_for_short_or_blank_terms(string term, bool expected)
    {
        Assert.Equal(expected, SearchService.IsVeryBroad(term));
    }
}
