using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BibleApp.Native.Models;

/// <summary>
/// The whole Bible, loaded once at startup and held in memory.
///
/// The Python original re-opened and re-read all 66 files on every search and
/// funnelled results through a temp file on disk. Loading ~4.3 MB once up front
/// costs a few hundred milliseconds at startup and makes every later operation
/// effectively instant.
/// </summary>
public sealed class BibleLibrary
{
    /// <summary>
    /// Canonical order, matching BibleFileNames.py. These are file names on disk;
    /// several are misspelled in the original data set and are deliberately left
    /// alone here so the files still resolve — see <see cref="DisplayNameFixes"/>.
    /// </summary>
    public static readonly string[] CanonicalFiles =
    [
        "genesis.txt", "exodus.txt", "leviticus.txt", "numbers.txt", "deuteronomy.txt",
        "joshua.txt", "judges.txt", "ruth.txt", "first_samuel.txt", "second_samuel.txt",
        "first_kings.txt", "second_kings.txt", "first_chronicles.txt", "second_chronicles.txt",
        "ezra.txt", "nehemiah.txt", "esther.txt", "job.txt", "psalms.txt", "proverbs.txt",
        "eccliasiastes.txt", "song_of_solomon.txt", "isaiah.txt", "jeremiah.txt",
        "lamentations.txt", "ezekial.txt", "daniel.txt", "hosea.txt", "joel.txt", "amos.txt",
        "obadiah.txt", "jonah.txt", "micah.txt", "nahum.txt", "habakkuk.txt", "zephaniah.txt",
        "haggai.txt", "zechariah.txt", "malachi.txt", "matthew.txt", "mark.txt", "luke.txt",
        "john.txt", "acts.txt", "romans.txt", "first_corinthians.txt", "second_corinthians.txt",
        "galatians.txt", "ephesians.txt", "philipians.txt", "colossians.txt",
        "first_thesselonians.txt", "second_thesselonians.txt", "first_timothy.txt",
        "second_timothy.txt", "titus.txt", "philemon.txt", "hebrews.txt", "james.txt",
        "first_peter.txt", "second_peter.txt", "first_john.txt", "second_john.txt",
        "third_john.txt", "jude.txt", "revelation.txt",
    ];

    /// <summary>
    /// Corrected spellings for books whose data files are misspelled. The file name
    /// stays as-is on disk; only what the user sees is fixed.
    /// </summary>
    private static readonly Dictionary<string, string> DisplayNameFixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["eccliasiastes.txt"] = "Ecclesiastes",
        ["ezekial.txt"] = "Ezekiel",
        ["philipians.txt"] = "Philippians",
        ["first_thesselonians.txt"] = "First Thessalonians",
        ["second_thesselonians.txt"] = "Second Thessalonians",
    };

    public required IReadOnlyList<BibleBook> Books { get; init; }

    /// <summary>Every verse in canonical order — the corpus the search runs over.</summary>
    public required IReadOnlyList<Verse> AllVerses { get; init; }

    public BibleBook? FindByFileName(string fileName) =>
        Books.FirstOrDefault(b => string.Equals(b.FileName, fileName, StringComparison.OrdinalIgnoreCase));

    public BibleBook? FindByDisplayName(string displayName) =>
        Books.FirstOrDefault(b => string.Equals(b.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));

    public static Task<BibleLibrary> LoadAsync() => Task.Run(Load);

    public static BibleLibrary Load()
    {
        var dataDir = ResolveDataDirectory();
        var books = new List<BibleBook>(CanonicalFiles.Length);
        var allVerses = new List<Verse>(32_000);

        foreach (var fileName in CanonicalFiles)
        {
            var path = Path.Combine(dataDir, fileName);
            if (!File.Exists(path)) continue;

            DisplayNameFixes.TryGetValue(fileName, out var corrected);
            var book = BibleBook.Parse(path, corrected);

            books.Add(book);
            allVerses.AddRange(book.Verses);
        }

        if (books.Count == 0)
            throw new FileNotFoundException(
                $"No Bible text files were found in \"{dataDir}\". The .txt files must sit " +
                "beside the executable, or in a \"Data\" folder next to it.");

        RedLetter.Apply(books, Path.Combine(dataDir, RedLetter.FileName));

        return new BibleLibrary { Books = books, AllVerses = allVerses };
    }

    /// <summary>
    /// The original hard-coded an absolute path from the author's machine. Look in the
    /// places the data can actually live instead: beside the exe, in a Data subfolder,
    /// or up the tree when running from a build output during development.
    /// </summary>
    private static string ResolveDataDirectory()
    {
        var baseDir = AppContext.BaseDirectory;

        var candidates = new List<string>
        {
            baseDir,
            Path.Combine(baseDir, "Data"),
        };

        var dir = new DirectoryInfo(baseDir);
        for (var i = 0; i < 5 && dir?.Parent is not null; i++)
        {
            dir = dir.Parent;
            candidates.Add(dir.FullName);
        }

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "genesis.txt")))
                return candidate;
        }

        return baseDir;
    }
}
