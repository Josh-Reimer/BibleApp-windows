using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BibleApp.Native.Models;
using BibleApp.Native.Services;

namespace BibleApp.Native;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const double MinFontSize = 9;
    private const double MaxFontSize = 32;

    private readonly UserSettings _settings = UserSettings.Load();

    private BibleLibrary? _bible;
    private List<DisplayLine>? _homeLines;

    /// <summary>Books the next search will cover. Narrowed by the search-range window.</summary>
    private IReadOnlyList<BibleBook> _searchRange = [];

    private BibleBook? _currentBook;
    private int _currentChapter;

    private CancellationTokenSource? _searchCancellation;

    /// <summary>Set while combo boxes are being repopulated in code, so that the
    /// SelectionChanged handlers do not fire against half-built state.</summary>
    private bool _suppressSelectionChanged;

    private double _readingFontSize = 14;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        ReadingFontSize = _settings.ReadingFontSize;
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;
        WindowState = _settings.WindowMaximized ? WindowState.Maximized : WindowState.Normal;

        if (Enum.TryParse<ThemeManager.Theme>(_settings.ThemeName, out var theme))
            ThemeManager.Apply(theme);
        SyncThemeMenu();

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    /// <summary>Reading pane font size. Bound to by the verse item template.</summary>
    public double ReadingFontSize
    {
        get => _readingFontSize;
        set
        {
            var clamped = Math.Clamp(value, MinFontSize, MaxFontSize);
            if (Math.Abs(clamped - _readingFontSize) < 0.01) return;
            _readingFontSize = clamped;
            OnPropertyChanged();
        }
    }

    // ==================== Startup / shutdown ====================

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _bible = await BibleLibrary.LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Bible", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
            return;
        }

        _searchRange = _bible.Books;
        RestoreSearchRange();

        _suppressSelectionChanged = true;
        BookCombo.ItemsSource = _bible.Books.Select(b => b.DisplayName).ToList();
        _suppressSelectionChanged = false;

        LoadingOverlay.Visibility = Visibility.Collapsed;

        // Reopen where the reader left off; first run lands on the whole-Bible view
        // the original always started with.
        var lastBook = _settings.LastBookFile is null ? null : _bible.FindByFileName(_settings.LastBookFile);
        if (lastBook is not null)
        {
            SelectBook(lastBook);
            if (_settings.LastChapter >= 1 && _settings.LastChapter <= lastBook.ChapterCount)
                SelectChapter(_settings.LastChapter);
        }
        else
        {
            ShowHome();
        }

        SearchBox.Focus();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _searchCancellation?.Cancel();

        _settings.LastBookFile = _currentBook?.FileName;
        _settings.LastChapter = _currentChapter;
        _settings.RangeStartFile = _searchRange.Count > 0 ? _searchRange[0].FileName : null;
        _settings.RangeEndFile = _searchRange.Count > 0 ? _searchRange[^1].FileName : null;
        _settings.ReadingFontSize = ReadingFontSize;
        _settings.ThemeName = ThemeManager.Current.ToString();
        _settings.WindowMaximized = WindowState == WindowState.Maximized;
        if (WindowState == WindowState.Normal)
        {
            _settings.WindowWidth = Width;
            _settings.WindowHeight = Height;
        }
        _settings.Save();
    }

    private void RestoreSearchRange()
    {
        if (_bible is null) return;
        if (_settings.RangeStartFile is null || _settings.RangeEndFile is null) return;

        var books = _bible.Books;
        var start = -1;
        var end = -1;
        for (var i = 0; i < books.Count; i++)
        {
            if (books[i].FileName == _settings.RangeStartFile) start = i;
            if (books[i].FileName == _settings.RangeEndFile) end = i;
        }

        if (start >= 0 && end >= start)
            _searchRange = _bible.Books.Skip(start).Take(end - start + 1).ToList();
    }

    // ==================== Views ====================

    private void ShowHome()
    {
        if (_bible is null) return;

        // Built once and reused — roughly 31,000 rows, all virtualized by the ListBox.
        _homeLines ??= _bible.Books
            .SelectMany(book => book.Content.Select(DisplayLine.FromBookLine))
            .ToList();

        _currentBook = null;
        _currentChapter = 0;

        _suppressSelectionChanged = true;
        BookCombo.SelectedIndex = -1;
        ChapterCombo.ItemsSource = null;
        VerseCombo.ItemsSource = null;
        _suppressSelectionChanged = false;

        VerseList.ItemsSource = _homeLines;
        VerseList.SelectedItem = null;
        ScrollToTop();
    }

    private void SelectBook(BibleBook book)
    {
        _currentBook = book;
        _currentChapter = 0;

        _suppressSelectionChanged = true;
        BookCombo.SelectedItem = book.DisplayName;
        ChapterCombo.ItemsSource = Enumerable.Range(1, book.ChapterCount).ToList();
        ChapterCombo.SelectedIndex = -1;
        VerseCombo.ItemsSource = null;
        _suppressSelectionChanged = false;

        VerseList.ItemsSource = book.Content.Select(DisplayLine.FromBookLine).ToList();
        VerseList.SelectedItem = null;
        ScrollToTop();
    }

    private void SelectChapter(int chapter)
    {
        if (_currentBook is null) return;

        _currentChapter = chapter;
        var verses = _currentBook.Chapter(chapter);

        _suppressSelectionChanged = true;
        ChapterCombo.SelectedItem = chapter;
        VerseCombo.ItemsSource = Enumerable.Range(1, verses.Count).ToList();
        VerseCombo.SelectedIndex = -1;
        _suppressSelectionChanged = false;

        VerseList.ItemsSource = verses.Select(v => DisplayLine.InBook(v)).ToList();
        VerseList.SelectedItem = null;
        ScrollToTop();
    }

    /// <summary>
    /// Where the original replaced the whole pane with a single verse, this keeps the
    /// chapter on screen and scrolls to the verse — the surrounding text is usually the
    /// reason someone looked the verse up.
    /// </summary>
    private void HighlightVerse(int verseNumber)
    {
        var target = (VerseList.ItemsSource as IEnumerable<DisplayLine>)?
            .FirstOrDefault(line => line.Verse is { } v
                                    && v.Number == verseNumber
                                    && v.Chapter == _currentChapter);
        if (target is null) return;

        VerseList.SelectedItem = target;
        VerseList.ScrollIntoView(target);
    }

    /// <summary>Jump from a search result to that verse in its chapter.</summary>
    private void GoToVerse(Verse verse)
    {
        SelectBook(verse.Book);
        SelectChapter(verse.Chapter);

        _suppressSelectionChanged = true;
        VerseCombo.SelectedItem = verse.Number;
        _suppressSelectionChanged = false;

        HighlightVerse(verse.Number);
    }

    private void ScrollToTop()
    {
        if (VerseList.Items.Count > 0)
            VerseList.ScrollIntoView(VerseList.Items[0]);
    }

    // ==================== Toolbar handlers ====================

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        SearchBox.Focus();
    }

    private void Home_Click(object sender, RoutedEventArgs e) => ShowHome();

    private void BookCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged || _bible is null) return;
        if (BookCombo.SelectedItem is not string displayName) return;

        var book = _bible.FindByDisplayName(displayName);
        if (book is not null) SelectBook(book);
    }

    private void ChapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged) return;
        if (ChapterCombo.SelectedItem is int chapter) SelectChapter(chapter);
    }

    private void VerseCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged) return;
        if (VerseCombo.SelectedItem is int verse) HighlightVerse(verse);
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            RunSearch();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            SearchBox.Clear();
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e) => RunSearch();

    // ==================== Search ====================

    private async void RunSearch()
    {
        if (_bible is null) return;

        var term = SearchBox.Text;

        // Carried over from the command-line version this GUI grew out of.
        if (term == "exit()") { Close(); return; }
        if (term.Length == 0) return;

        if (SearchService.IsVeryBroad(term))
        {
            var answer = MessageBox.Show(
                "A search this short matches a very large part of the Bible, and the results " +
                "will be hard to read. Search anyway?",
                "Bible", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (answer != MessageBoxResult.OK) return;
        }

        _searchCancellation?.Cancel();
        _searchCancellation = new CancellationTokenSource();
        var token = _searchCancellation.Token;

        SearchButton.IsEnabled = false;
        CounterBox.Text = $"searching for '{term}'…";

        SearchService.Result result;
        try
        {
            result = await SearchService.SearchAsync(_searchRange, term, token);
        }
        catch (OperationCanceledException)
        {
            return;   // A newer search took over; it owns the UI now.
        }
        finally
        {
            if (!token.IsCancellationRequested) SearchButton.IsEnabled = true;
        }

        ShowResults(term, result);
    }

    private void ShowResults(string term, SearchService.Result result)
    {
        _currentBook = null;
        _currentChapter = 0;

        _suppressSelectionChanged = true;
        BookCombo.SelectedIndex = -1;
        ChapterCombo.ItemsSource = null;
        VerseCombo.ItemsSource = null;
        _suppressSelectionChanged = false;

        if (result.TotalMatches == 0)
        {
            CounterBox.Text = $"no results found for '{term}'";
            VerseList.ItemsSource = new[]
            {
                DisplayLine.Heading(
                    $"Nothing in the {RangeDescription()} matches “{term}”. " +
                    "Try different spacing or punctuation — the search is not case sensitive."),
            };
            return;
        }

        CounterBox.Text = result.Truncated
            ? $"showing first {result.Hits.Count:N0} of {result.TotalMatches:N0} results for '{term}'"
            : result.TotalMatches == 1
                ? $"1 result found for '{term}'"
                : $"{result.TotalMatches:N0} results found for '{term}'";

        VerseList.ItemsSource = result.Hits
            .Select(hit => DisplayLine.InResults(hit.Verse, term))
            .ToList();
        VerseList.SelectedItem = null;
        ScrollToTop();
    }

    private string RangeDescription() =>
        _searchRange.Count == 0 || (_bible is not null && _searchRange.Count == _bible.Books.Count)
            ? "Bible"
            : $"range {_searchRange[0].DisplayName}–{_searchRange[^1].DisplayName}";

    private void VerseList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (VerseList.SelectedItem is DisplayLine { Verse: { } verse })
            GoToVerse(verse);
    }

    // ==================== Search range ====================

    private void SearchRange_Click(object sender, RoutedEventArgs e)
    {
        if (_bible is null) return;

        var dialog = new RangeWindow(_bible, _searchRange) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.SelectedRange is not null)
        {
            _searchRange = dialog.SelectedRange;
            CounterBox.Text = _searchRange.Count == _bible.Books.Count
                ? "search range: the whole Bible"
                : $"search range: {_searchRange[0].DisplayName} to {_searchRange[^1].DisplayName}";
        }
    }

    // ==================== Clipboard ====================

    private void SearchCopy_Click(object sender, RoutedEventArgs e)
    {
        var text = SearchBox.SelectionLength > 0 ? SearchBox.SelectedText : SearchBox.Text;
        TrySetClipboard(text);
    }

    private void SearchPaste_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Clipboard.ContainsText()) SearchBox.Text = Clipboard.GetText();
        }
        catch (Exception)
        {
            // The clipboard is a shared OS resource and can briefly be locked by
            // another process. Nothing useful to tell the reader.
        }
    }

    private void CopyVerse_Click(object sender, RoutedEventArgs e)
    {
        if (VerseList.SelectedItem is DisplayLine line)
            TrySetClipboard(line.Text);
    }

    private void CopyVerseWithReference_Click(object sender, RoutedEventArgs e)
    {
        if (VerseList.SelectedItem is DisplayLine { Verse: { } verse } line)
            TrySetClipboard($"{verse.Reference}  {line.Text}");
    }

    private static void TrySetClipboard(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        try { Clipboard.SetText(text); }
        catch (Exception) { /* see SearchPaste_Click */ }
    }

    // ==================== View options ====================

    private void FocusSearch_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    // Each of these is reachable two ways — a keyboard command and a menu or button —
    // and the two require different delegate signatures.
    private void Home_Executed(object sender, ExecutedRoutedEventArgs e) => ShowHome();

    private void IncreaseFont_Executed(object sender, ExecutedRoutedEventArgs e) => ReadingFontSize += 1;

    private void DecreaseFont_Executed(object sender, ExecutedRoutedEventArgs e) => ReadingFontSize -= 1;

    private void IncreaseFont_Click(object sender, RoutedEventArgs e) => ReadingFontSize += 1;

    private void DecreaseFont_Click(object sender, RoutedEventArgs e) => ReadingFontSize -= 1;

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string name }) return;
        if (!Enum.TryParse<ThemeManager.Theme>(name, out var theme)) return;

        ThemeManager.Apply(theme);
        SyncThemeMenu();
    }

    private void SyncThemeMenu()
    {
        ThemeGreen.IsChecked = ThemeManager.Current == ThemeManager.Theme.Green;
        ThemeDarkGray.IsChecked = ThemeManager.Current == ThemeManager.Theme.DarkGray;
        ThemeDarkAmber.IsChecked = ThemeManager.Current == ThemeManager.Theme.DarkAmber;
    }

    // ==================== INotifyPropertyChanged ====================

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
