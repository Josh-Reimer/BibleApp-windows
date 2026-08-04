using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BibleApp.Native.Services;

/// <summary>
/// Small amount of state persisted between runs, in
/// %APPDATA%\BibleApp\settings.json. The original app remembered nothing.
/// </summary>
public sealed class UserSettings
{
    public string? LastBookFile { get; set; }
    public int LastChapter { get; set; }
    public string? RangeStartFile { get; set; }
    public string? RangeEndFile { get; set; }
    public double ReadingFontSize { get; set; } = 14;
    public string ThemeName { get; set; } = "Green";
    public double WindowWidth { get; set; } = 1000;
    public double WindowHeight { get; set; } = 700;
    public bool WindowMaximized { get; set; } = true;

    [JsonIgnore]
    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BibleApp",
        "settings.json");

    public static UserSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(FilePath))
                       ?? new UserSettings();
        }
        catch (Exception)
        {
            // A corrupt or unreadable settings file should never stop the app opening.
        }
        return new UserSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception)
        {
            // Settings are a convenience; failing to persist them is not worth an error dialog.
        }
    }
}
