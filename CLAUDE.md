# CLAUDE.md

Desktop KJV Bible reader with keyword search. Two implementations live side by side:

| | |
|---|---|
| **Python / PySimpleGUI** (repo root) | The original. `bibleGUI.py` is the GUI, `connector.py` the command-line version. |
| **C# / WPF** (`BibleApp.Native/`) | Native Windows port on the `native-wpf` branch. See its own README for the porting notes. |

Both read the same 66 `.txt` files at the repository root.

## The data files

One verse per line, `chapter:verse: text`:

```
1:1: In the beginning God created the heaven and the earth.
```

Lines that don't match that shape are heading text ("GENESIS", "The First Book of Moses
called"). Most appear at the top of a file; they are not exclusively there.

`Bible.txt` is a 4.3 MB concatenation of all 66 books. The Python version loads it for the
whole-Bible view; the WPF version assembles that view from the individual books instead and
does not ship it.

### Known defects in the data — verified, not guesses

Anything that parses these files has to cope with all four:

- **2 Corinthians 1:1 is absent.** `second_corinthians.txt` begins at `1:2:`. Not a parsing
  problem — the verse is not in the file. Left unfixed deliberately: repairing it means
  writing scripture text into the data, which is the repo owner's call.
- **Galatians 6:15 and 6:16 share one line** (`galatians.txt:146`), a lost newline. The
  Python version silently drops 6:16. The WPF parser splits on an embedded `N:M:` marker,
  but only when it continues that line's own chapter and verse sequence.
- **1 Thessalonians 1:1 is indented** — `" 1:1: Paul, and Silvanus..."`. Python's `int()`
  skips leading whitespace, so the original copes by accident. Any regex anchored hard to
  `^` will silently drop the verse.
- **Isaiah has a mid-file divider** — `ISAIAH Chapters 34-66` at `isaiah.txt:651`.

The WPF parser reads 31,098 verses. A standard KJV has 31,102; 2 Cor 1:1 accounts for one.
A gap check can't detect a verse missing from the *end* of a chapter, which is the likely
shape of the rest. All 66 books do have correct chapter counts.

### Misspelled filenames are load-bearing

`eccliasiastes.txt`, `ezekial.txt`, `philipians.txt`, `first_thesselonians.txt` and
`second_thesselonians.txt` are misspelled on disk, and `BibleFileNames.py` refers to them by
those names. **Don't rename them.** The WPF version maps them to correct spellings for
display only, in `BibleLibrary.DisplayNameFixes`.

Correctly spelled duplicates (`philippians.txt`, `first_thessalonians.txt`,
`second_thessalonians.txt`) also exist and are unused — the WPF `.csproj` excludes them.

## Running

**Python.** `python bibleGUI.py` (GUI) or `python connector.py` (CLI). Needs
`pysimplegui`, `pyperclip`. Both must run with the repo root as the working directory —
paths to the `.txt` files are relative and unqualified.

> `bibleGUI.py` imports `asyncio.windows_events` and `msilib.schema` at lines 2–3. Neither is
> used, both are Windows-only, and `msilib` was removed in Python 3.13 — so the file will not
> import on a current Python until those lines go.

**WPF.** Requires the .NET 9 SDK **on Windows**; it does not build on macOS or Linux.

```
cd BibleApp.Native
dotnet build
dotnet run
```

Self-contained single-file publish:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Distribution

Shipped as a PyInstaller exe wrapped in an Inno Setup installer. The `.iss` script is not in
this repo. Two things matter if you touch it: keep `AppId` constant across versions or
installs stack up instead of upgrading, and prefer a per-user install
(`PrivilegesRequired=lowest` with `{autopf}`) so updates apply without a UAC prompt.

## Conventions

- The Python modules carry `DO NOT DELETE` banners in their docstrings. They mean it —
  `BibleFileNames.py`, `ChapterVerse.py`, `charEliminator.py` and `list_and_str_ops.py` are
  each imported by several scripts.
- `bibleSearchResult.txt` is a scratch file the Python version writes results into and
  re-reads. It is expected to be empty at rest. The WPF version doesn't use it.
- The Python GUI hard-codes `C:\Users\Joel\Documents\...\blackbible.ico` for window icons, so
  icons are missing on any other machine. The WPF version uses relative resource paths.
