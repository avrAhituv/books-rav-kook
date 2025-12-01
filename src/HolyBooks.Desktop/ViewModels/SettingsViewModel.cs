using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HolyBooks.Core.Models;
using HolyBooks.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace HolyBooks.Desktop.ViewModels;

/// <summary>
/// ViewModel להגדרות האפליקציה
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    // Appearance
    [ObservableProperty]
    private string _theme = "light";

    [ObservableProperty]
    private int _fontSize = 18;

    [ObservableProperty]
    private string _fontFamily = "David";

    [ObservableProperty]
    private double _lineHeight = 1.8;

    [ObservableProperty]
    private string _textWidth = "medium";

    // Reading
    [ObservableProperty]
    private bool _showVerseNumbers = true;

    [ObservableProperty]
    private bool _highlightSearchResults = true;

    [ObservableProperty]
    private bool _autoSaveSession = true;

    [ObservableProperty]
    private bool _rememberLastPosition = true;

    // Available options
    public ObservableCollection<string> AvailableThemes { get; } = new()
    {
        "light", "dark", "sepia"
    };

    public ObservableCollection<string> AvailableFonts { get; } = new()
    {
        "David", "Frank Ruhl Libre", "Noto Serif Hebrew", "Segoe UI"
    };

    public ObservableCollection<string> AvailableTextWidths { get; } = new()
    {
        "narrow", "medium", "wide"
    };

    // Cache info
    [ObservableProperty]
    private string _cacheSize = "מחשב...";

    [ObservableProperty]
    private int _cachedTextsCount;

    // About
    public string Version => "1.0.0";
    public string BuildDate => DateTime.Now.ToString("yyyy-MM-dd");

    public SettingsViewModel(AppDbContext db)
    {
        _db = db;
        LoadSettings();
    }

    private async void LoadSettings()
    {
        var settings = await _db.AppSettings.ToListAsync();

        Theme = GetSetting(settings, SettingsKeys.Theme, "light");
        FontSize = int.Parse(GetSetting(settings, SettingsKeys.FontSize, "18"));
        FontFamily = GetSetting(settings, SettingsKeys.FontFamily, "David");
        LineHeight = double.Parse(GetSetting(settings, SettingsKeys.LineHeight, "1.8"));
        TextWidth = GetSetting(settings, SettingsKeys.TextWidth, "medium");
        ShowVerseNumbers = bool.Parse(GetSetting(settings, SettingsKeys.ShowVerseNumbers, "true"));
        HighlightSearchResults = bool.Parse(GetSetting(settings, SettingsKeys.HighlightSearchResults, "true"));
        AutoSaveSession = bool.Parse(GetSetting(settings, SettingsKeys.AutoSaveSession, "true"));

        await UpdateCacheInfo();
    }

    private string GetSetting(List<AppSettings> settings, string key, string defaultValue)
    {
        return settings.FirstOrDefault(s => s.Key == key)?.Value ?? defaultValue;
    }

    [RelayCommand]
    public async Task SaveSettings()
    {
        await SaveSetting(SettingsKeys.Theme, Theme);
        await SaveSetting(SettingsKeys.FontSize, FontSize.ToString());
        await SaveSetting(SettingsKeys.FontFamily, FontFamily);
        await SaveSetting(SettingsKeys.LineHeight, LineHeight.ToString());
        await SaveSetting(SettingsKeys.TextWidth, TextWidth);
        await SaveSetting(SettingsKeys.ShowVerseNumbers, ShowVerseNumbers.ToString());
        await SaveSetting(SettingsKeys.HighlightSearchResults, HighlightSearchResults.ToString());
        await SaveSetting(SettingsKeys.AutoSaveSession, AutoSaveSession.ToString());

        await _db.SaveChangesAsync();
    }

    private async Task SaveSetting(string key, string value)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);

        if (setting != null)
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.AppSettings.Add(new AppSettings
            {
                Key = key,
                Value = value
            });
        }
    }

    [RelayCommand]
    public async Task ClearCache()
    {
        // Clear Sefaria cache
        _db.SefariaCache.RemoveRange(_db.SefariaCache);
        await _db.SaveChangesAsync();

        await UpdateCacheInfo();
    }

    [RelayCommand]
    public async Task ClearReadingHistory()
    {
        _db.ReadingHistory.RemoveRange(_db.ReadingHistory);
        await _db.SaveChangesAsync();
    }

    [RelayCommand]
    public async Task ExportData()
    {
        // TODO: Implement data export (bookmarks, notes, sheets)
    }

    [RelayCommand]
    public async Task ImportData()
    {
        // TODO: Implement data import
    }

    private async Task UpdateCacheInfo()
    {
        CachedTextsCount = await _db.SefariaCache.CountAsync();

        // Estimate cache size
        var totalSize = await _db.SefariaCache
            .SumAsync(c => (long)c.ContentJson.Length);

        CacheSize = FormatFileSize(totalSize);
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    // Property change handlers for auto-save
    partial void OnThemeChanged(string value) => _ = SaveSettings();
    partial void OnFontSizeChanged(int value) => _ = SaveSettings();
    partial void OnFontFamilyChanged(string value) => _ = SaveSettings();
    partial void OnLineHeightChanged(double value) => _ = SaveSettings();
    partial void OnTextWidthChanged(string value) => _ = SaveSettings();
}
