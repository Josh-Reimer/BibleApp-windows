# BibleApp — native Windows version

A WPF (.NET 9, C#) port of `bibleGUI.py`. Same layout, same green theme, same controls in
the same order — the Python/PySimpleGUI implementation underneath is replaced.

The original is untouched on `main`; this lives on the `native-wpf` branch.

## Building

Requires the .NET 9 SDK **on Windows** — WPF does not build or run on macOS or Linux.

```
cd BibleApp.Native
dotnet build
dotnet run
```

A self-contained exe with no .NET install required on the target machine:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The 66 KJV `.txt` files are copied from the repository root into the output folder by the
`Content` item in the .csproj. `Bible.txt` is deliberately not shipped — the whole-Bible
view is assembled from the individual books, so including it would duplicate the entire
corpus for nothing.

## What was kept

The toolbar is the same row of controls in the same order: the "enter search keywords:"
label, the search box, `clear`, the home button (same icon, extracted from the base64 blob
in `bibleGUI.py`), `SEARCH`, the grey result-count box, and the book / chapter / verse
combo boxes. The reading pane below is still monospace text on the theme's input colour.
Right-click menus remain the primary way to reach copy, paste and the search range, as in
the original. `SEARCH` still fires on Enter, and typing `exit()` still quits.

The green theme is PySimpleGUI's `green` transcribed into `Themes/Green.xaml` — six
brushes, easy to adjust if a shade looks wrong.

## What changed, and why

**The Bible is parsed once at startup, not on every search.** The original reopened and
re-read all 66 files for each search and passed results through `bibleSearchResult.txt` on
disk. Everything is now in memory: about 300 ms at startup, and searches complete in
single-digit milliseconds. The scratch file is gone.

**Search runs off the UI thread and is cancellable.** A new search supersedes one already
running, and the window never freezes. The original's "you are about to initiate a long
search" prompt now only warns that a one- or two-character term returns an unreadably
large result set, since the delay it was really about no longer exists.

**Search matches verse text only.** The original tested the whole raw line, so searching
`1:1` matched every verse whose reference prefix contained it.

**Results are structured, and the search term is highlighted.** Each hit shows its full
reference (`Genesis 1:1`) rather than the book name on a separate line, and double-clicking
a result jumps to that verse in its chapter.

**Choosing a verse scrolls to it instead of replacing the pane.** The original replaced the
whole reading pane with a single verse; the surrounding text is usually the reason someone
looked a verse up.

**The reading pane is virtualised.** The whole-Bible view is ~31,000 rows and stays
responsive; the original pushed a 4.3 MB string into one Tk text widget.

**Resource paths are relative.** The original hard-coded
`C:\Users\Joel\Documents\...\blackbible.ico` for every window icon and popup, so the icon
was missing on any other machine.

**Position, theme, font size and search range persist** in
`%APPDATA%\BibleApp\settings.json`.

**The search-range dialog validates as you type.** Invalid ranges disable `set range` and
explain why, instead of the original's three separate popups on submit. It also opens
showing the range currently in effect, and "reset to defuault" is spelled correctly.

**Extras:** Ctrl+F to focus search, Esc to clear, Ctrl+Home for the whole Bible,
Ctrl+/Ctrl- for font size, "copy with reference", and a theme switcher offering the three
themes `bibleGUI.py`'s comments named as favourites (green, dark gray, dark amber).

**Misspelled book names are corrected in the UI only.** `eccliasiastes.txt`,
`ezekial.txt`, `philipians.txt` and the two `thesselonians` files keep their names on disk;
`BibleLibrary.DisplayNameFixes` maps them to the correct spellings for display.

### Fonts

The original asked for `Fixedsys` on the buttons. That is a Windows 3.1 raster font, and
WPF renders only TrueType/OpenType, so it would silently fall back to the default UI font.
Consolas SemiBold is used instead. `Corbel` and `Consolas` are unchanged.

## Data defects found while porting

Validating the parser against the text files turned up problems that affect the Python
version too:

| | |
|---|---|
| **2 Corinthians 1:1 is missing** | `second_corinthians.txt` begins at `1:2:`. The verse is absent from the file. Not fixed here — that means writing scripture text into your data, which is your call. |
| **Galatians 6:15 and 6:16 share a line** | `galatians.txt:146` lost a newline, so the Python version silently swallows 6:16. The parser now splits a line on an embedded marker, but only when it continues that line's own chapter and verse numbering. |
| **1 Thessalonians 1:1 is indented** | Stored as `" 1:1: Paul, and Silvanus..."`. Python's `int()` skips leading whitespace so the original coped by accident; the parser tolerates it explicitly. |
| **Isaiah has a mid-file divider** | `isaiah.txt:651` carries `ISAIAH Chapters 34-66` between verses, so heading lines are preserved wherever they occur rather than only at the top of a file. |

After these changes the parser reads **31,098 verses**. A standard KJV has 31,102, and
2 Corinthians 1:1 accounts for one of the four. A gap check cannot detect a verse missing
from the *end* of a chapter, which is the likely shape of the remainder — finding those
needs a reference text to diff against. All 66 books do have the correct chapter counts.

## Layout

```
Models/       BibleBook (parser), BibleLibrary (66 books + verse index),
              Verse, DisplayLine (one row in the reading pane)
Services/     SearchService (cancellable, off-thread), UserSettings
Controls/     VerseTextBehavior — builds the reference/text/highlight runs
              for a TextBlock, since TextBlock.Inlines is not bindable
Themes/       Green, DarkGray, DarkAmber colours + shared control styles
```
