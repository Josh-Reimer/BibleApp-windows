using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BibleApp.Native.Models;

namespace BibleApp.Native;

/// <summary>
/// Narrows searches to a contiguous span of books, as the original's
/// "select range to search" window did.
/// </summary>
public partial class RangeWindow : Window
{
    private readonly BibleLibrary _bible;

    public IReadOnlyList<BibleBook>? SelectedRange { get; private set; }

    public RangeWindow(BibleLibrary bible, IReadOnlyList<BibleBook> currentRange)
    {
        _bible = bible;
        InitializeComponent();

        var names = bible.Books.Select(b => b.DisplayName).ToList();
        StartCombo.ItemsSource = names;
        EndCombo.ItemsSource = names;

        // Open showing the range already in effect rather than two blank boxes.
        if (currentRange.Count > 0)
        {
            StartCombo.SelectedItem = currentRange[0].DisplayName;
            EndCombo.SelectedItem = currentRange[^1].DisplayName;
        }

        Validate();
    }

    private void Combo_SelectionChanged(object sender, SelectionChangedEventArgs e) => Validate();

    private void Validate()
    {
        var startIndex = StartCombo.SelectedIndex;
        var endIndex = EndCombo.SelectedIndex;

        if (startIndex < 0 || endIndex < 0)
        {
            ValidationMessage.Text = "Choose a starting book and an ending book.";
            SetRangeButton.IsEnabled = false;
        }
        else if (startIndex > endIndex)
        {
            ValidationMessage.Text =
                "The ending book comes before the starting book. Swap them to set a range.";
            SetRangeButton.IsEnabled = false;
        }
        else
        {
            var count = endIndex - startIndex + 1;
            ValidationMessage.Text = count == _bible.Books.Count
                ? "Searching the whole Bible."
                : $"Searching {count} book{(count == 1 ? "" : "s")}.";
            SetRangeButton.IsEnabled = true;
        }
    }

    private void SetRange_Click(object sender, RoutedEventArgs e)
    {
        var startIndex = StartCombo.SelectedIndex;
        var endIndex = EndCombo.SelectedIndex;
        if (startIndex < 0 || endIndex < startIndex) return;

        SelectedRange = _bible.Books.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
        DialogResult = true;
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        SelectedRange = _bible.Books;
        DialogResult = true;
    }
}
