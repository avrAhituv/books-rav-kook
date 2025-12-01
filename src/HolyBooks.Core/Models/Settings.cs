namespace HolyBooks.Core.Models;

/// <summary>
/// הגדרות אפליקציה
/// </summary>
public class AppSettings
{
    public int Id { get; set; }

    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache לספריא
/// </summary>
public class SefariaCache
{
    public int Id { get; set; }

    /// <summary>
    /// הפניה: "Genesis.1.1"
    /// </summary>
    public string Ref { get; set; } = string.Empty;

    /// <summary>
    /// תוכן JSON מלא
    /// </summary>
    public string ContentJson { get; set; } = string.Empty;

    /// <summary>
    /// סוג: "text", "commentary", "link"
    /// </summary>
    public string ContentType { get; set; } = "text";

    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// זמן תפוגה (null = לעולם)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// קבועים להגדרות
/// </summary>
public static class SettingsKeys
{
    public const string FontSize = "font_size";
    public const string FontFamily = "font_family";
    public const string Theme = "theme"; // "light", "dark", "sepia"
    public const string LineHeight = "line_height";
    public const string TextWidth = "text_width"; // "narrow", "medium", "wide"
    public const string LastBookId = "last_book_id";
    public const string LastContentId = "last_content_id";
    public const string AutoSaveSession = "auto_save_session";
    public const string ShowVerseNumbers = "show_verse_numbers";
    public const string HighlightSearchResults = "highlight_search_results";
}

/// <summary>
/// הגדרות ברירת מחדל
/// </summary>
public static class DefaultSettings
{
    public static Dictionary<string, string> Values = new()
    {
        { SettingsKeys.FontSize, "18" },
        { SettingsKeys.FontFamily, "David" },
        { SettingsKeys.Theme, "light" },
        { SettingsKeys.LineHeight, "1.8" },
        { SettingsKeys.TextWidth, "medium" },
        { SettingsKeys.AutoSaveSession, "true" },
        { SettingsKeys.ShowVerseNumbers, "true" },
        { SettingsKeys.HighlightSearchResults, "true" }
    };
}
