using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BibleApp.Native.Models;

namespace BibleApp.Native.Services;

/// <summary>
/// Case-insensitive substring search over an in-memory verse list.
///
/// Differences from the Python original, all deliberate:
///   * runs off the UI thread and is cancellable, so a slow search never freezes
///     the window and a new search supersedes one already in flight;
///   * matches against verse text only, not the "3:16: " reference prefix, so
///     searching "1:1" no longer returns every verse whose prefix happens to match;
///   * returns structured hits instead of a concatenated string written to a temp file.
/// </summary>
public static class SearchService
{
    /// <summary>Cap on results held in memory. The pathological case is a
    /// single-space search, which matches essentially every verse.</summary>
    public const int MaxResults = 5000;

    public sealed record Hit(Verse Verse, int MatchStart, int MatchLength);

    public sealed record Result(IReadOnlyList<Hit> Hits, int TotalMatches, bool Truncated);

    public static Task<Result> SearchAsync(
        IReadOnlyList<BibleBook> range,
        string term,
        CancellationToken cancellationToken) =>
        Task.Run(() => Search(range, term, cancellationToken), cancellationToken);

    public static Result Search(
        IReadOnlyList<BibleBook> range,
        string term,
        CancellationToken cancellationToken = default)
    {
        var hits = new List<Hit>();
        var total = 0;

        if (string.IsNullOrEmpty(term))
            return new Result(hits, 0, false);

        foreach (var book in range)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var verse in book.Verses)
            {
                var index = verse.Text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (index < 0) continue;

                total++;
                if (hits.Count < MaxResults)
                    hits.Add(new Hit(verse, index, term.Length));
            }
        }

        return new Result(hits, total, total > hits.Count);
    }

    /// <summary>
    /// The original warned before searching blank or very short terms because those
    /// searches took seconds. In memory they take milliseconds, so the warning is gone —
    /// but a term this broad still returns a uselessly large result set, which is worth
    /// saying up front.
    /// </summary>
    public static bool IsVeryBroad(string term) =>
        string.IsNullOrWhiteSpace(term) || term.Trim().Length <= 2;
}
