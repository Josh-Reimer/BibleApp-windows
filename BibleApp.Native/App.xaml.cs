using System;
using System.Windows;

namespace BibleApp.Native;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // A stack trace dialog is not useful to a reader looking something up; a
        // sentence saying what went wrong is.
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                args.Exception.Message,
                "Bible", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}

/// <summary>Swaps the active theme dictionary at runtime.</summary>
public static class ThemeManager
{
    public enum Theme { Green, DarkGray, DarkAmber }

    public static Theme Current { get; private set; } = Theme.Green;

    public static void Apply(Theme theme)
    {
        // A relative URI resolves against the entry assembly, which is only this one when
        // the app is launched normally — it breaks the moment anything else hosts these
        // windows. Name the assembly holding the dictionaries explicitly.
        var assembly = typeof(ThemeManager).Assembly.GetName().Name;
        var uri = new Uri(
            $"pack://application:,,,/{assembly};component/Themes/{theme}.xaml",
            UriKind.Absolute);
        var dictionary = new ResourceDictionary { Source = uri };

        // Index 0 by convention — see App.xaml.
        Application.Current.Resources.MergedDictionaries[0] = dictionary;
        Current = theme;
    }
}
