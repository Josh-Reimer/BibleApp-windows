# CLAUDE.md

Desktop KJV Bible reader with keyword search. **Which language you are looking at depends on
the branch:**

| Branch | Source |
|---|---|
| `main` | **Python only.** The original: `bibleGUI.py` is the GUI, `connector.py` the command-line version. |
| `native-wpf` | **Both.** The Python original is untouched at the repo root, and a C# / WPF port sits alongside it in `BibleApp.Native/`, with xUnit tests in `BibleApp.Tests/`. |

`BibleApp.Native/` and `BibleApp.Tests/` therefore do not exist on `main` — check the branch
before concluding a path is missing. The C# port is the one that currently has an installer;
`BibleApp.Native/README.md` covers what the port kept, what it changed, and why.

Both implementations read the same 66 `.txt` files at the repository root.

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

Startup parses all 66 files into memory in roughly 300 ms; searches then run in single-digit
milliseconds. If either number moves by an order of magnitude, something has regressed to
per-search file I/O — which is what the Python version did.

Self-contained single-file publish:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

**Tests.** `cd BibleApp.Tests && dotnet test` — 53 xUnit tests, about half a second.

Unlike the app, these are **not** Windows-only. The test project targets plain `net9.0` and
links the parser and search in as source rather than referencing the WPF assembly, so they
run anywhere the SDK does. Nothing under `Models/` or `Services/SearchService.cs` touches
WPF; keep it that way, or the tests get pinned to Windows along with the app.

`DataDefectTests` is the important one — it pins each of the four data defects described
above, so a change to the data or to the parsing rules has to be deliberate rather than
silent. It also asserts that 2 Corinthians 1 is the *only* chapter in all 66 books with a
numbering gap.

## Distribution

The Python version shipped as a PyInstaller exe wrapped in an Inno Setup installer whose
`.iss` was never in this repo.

The WPF version has one: `BibleApp.Native/Installer/BibleApp.iss`. Publish first, then
compile — the script reads the version out of the published exe:

```
cd BibleApp.Native
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
iscc /DArch=x64 Installer\BibleApp.iss
```

Output lands in `dist-installer/`. Use `-r win-arm64` with `/DArch=arm64` for ARM64 Windows.
A self-contained WPF build is about 134 MB, compressing to a ~36 MB installer; WPF cannot be
trimmed, so that floor is not going anywhere.

Four things in that script are load-bearing:

- **`AppId` must never change.** It is what makes a later install an upgrade instead of a
  second entry in Add/Remove Programs.
- **The install is per-user** (`PrivilegesRequired=lowest` with `{autopf}`, landing in
  `%LocalAppData%\Programs\BibleApp`), so updates apply with no UAC prompt. A prompt on
  every update is a prompt people learn to cancel. `/ALLUSERS` overrides it.
- **`<Version>` in the .csproj is the single source of truth.** The `.iss` reads it back out
  of the compiled exe rather than keeping its own copy. Two copies drift, and an updater
  comparing the running assembly's version against the published release then either never
  updates or updates forever.
- **`CloseApplications=yes`** lets Restart Manager shut a running copy down before
  overwriting it. Verified: a running instance is closed by a silent upgrade.

For an auto-updater, invoke it as
`BibleApp-setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /relaunch=yes` — the
`/relaunch=yes` parameter is handled in the script's `[Code]` section and brings the app
back up afterwards. A plain silent install stays quiet.

Not yet done: code signing. Unsigned, both the installer and an updater downloading it will
hit SmartScreen, and reputation resets with each new build.

## Conventions

- The Python modules carry `DO NOT DELETE` banners in their docstrings. They mean it —
  `BibleFileNames.py`, `ChapterVerse.py`, `charEliminator.py` and `list_and_str_ops.py` are
  each imported by several scripts.
- `bibleSearchResult.txt` is a scratch file the Python version writes results into and
  re-reads. It is expected to be empty at rest. The WPF version doesn't use it.
- The Python GUI hard-codes `C:\Users\Joel\Documents\...\blackbible.ico` for window icons, so
  icons are missing on any other machine. The WPF version uses relative resource paths.
