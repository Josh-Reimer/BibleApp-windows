using BibleApp.Native.Models;

namespace BibleApp.Tests;

/// <summary>
/// Parses the 66 text files once and shares the result across every test class.
/// Loading takes roughly 300 ms, which is worth paying once rather than per class.
/// </summary>
public sealed class BibleFixture
{
    public BibleLibrary Bible { get; } = BibleLibrary.Load();

    /// <summary>Looks up a verse, or null when it is not in the data.</summary>
    public Verse? Find(string fileName, int chapter, int verse) =>
        Bible.FindByFileName(fileName)?.Chapter(chapter).FirstOrDefault(v => v.Number == verse);

    public BibleBook Book(string fileName) =>
        Bible.FindByFileName(fileName)
        ?? throw new InvalidOperationException($"{fileName} did not load");
}

[CollectionDefinition(Name)]
public sealed class BibleCollection : ICollectionFixture<BibleFixture>
{
    public const string Name = "bible";
}
