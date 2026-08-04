namespace BibleApp.Native.Models;

/// <summary>
/// A single verse. The source files store one verse per line in the form
/// "chapter:verse: text" — see <see cref="BibleBook.Parse"/>.
/// </summary>
public sealed class Verse
{
    public required BibleBook Book { get; init; }
    public required int Chapter { get; init; }
    public required int Number { get; init; }
    public required string Text { get; init; }

    /// <summary>e.g. "Genesis 1:1" — used for the reference gutter and clipboard.</summary>
    public string Reference => $"{Book.DisplayName} {Chapter}:{Number}";

    public override string ToString() => $"{Reference}  {Text}";
}
