namespace HolyBooks.Core.Models;

/// <summary>
/// סימניה
/// </summary>
public class Bookmark
{
    public int Id { get; set; }

    public int ContentId { get; set; }
    public Content Content { get; set; } = null!;

    /// <summary>
    /// מיקום בטקסט (offset)
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// שם הסימניה
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// צבע הסימניה
    /// </summary>
    public string Color { get; set; } = "#FFD700"; // Gold

    /// <summary>
    /// תיקייה
    /// </summary>
    public int? FolderId { get; set; }
    public BookmarkFolder? Folder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// תיקיית סימניות
/// </summary>
public class BookmarkFolder
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? ParentId { get; set; }
    public BookmarkFolder? Parent { get; set; }

    public string? Icon { get; set; }
    public int SortOrder { get; set; }

    public ICollection<BookmarkFolder> Children { get; set; } = new List<BookmarkFolder>();
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
}

/// <summary>
/// הערה או הדגשה
/// </summary>
public class Note
{
    public int Id { get; set; }

    public int ContentId { get; set; }
    public Content Content { get; set; } = null!;

    /// <summary>
    /// תחילת הסימון
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// סוף הסימון
    /// </summary>
    public int EndPosition { get; set; }

    /// <summary>
    /// תוכן ההערה
    /// </summary>
    public string? NoteText { get; set; }

    /// <summary>
    /// סוג: "note", "highlight"
    /// </summary>
    public string NoteType { get; set; } = "note";

    /// <summary>
    /// צבע ההדגשה
    /// </summary>
    public string Color { get; set; } = "#FFFF00"; // Yellow

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // תגיות
    public ICollection<NoteTag> Tags { get; set; } = new List<NoteTag>();
}

/// <summary>
/// תגית להערה
/// </summary>
public class NoteTag
{
    public int Id { get; set; }

    public int NoteId { get; set; }
    public Note Note { get; set; } = null!;

    public string Tag { get; set; } = string.Empty;
}

/// <summary>
/// היסטוריית קריאה
/// </summary>
public class ReadingHistory
{
    public int Id { get; set; }

    public int ContentId { get; set; }
    public Content Content { get; set; } = null!;

    /// <summary>
    /// אחוז גלילה (0-1)
    /// </summary>
    public double ScrollPosition { get; set; }

    /// <summary>
    /// זמן קריאה אחרון
    /// </summary>
    public DateTime LastRead { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// משך קריאה בשניות
    /// </summary>
    public int ReadingTimeSeconds { get; set; }
}

/// <summary>
/// סשן קריאה (שמירת טאבים)
/// </summary>
public class ReadingSession
{
    public int Id { get; set; }

    /// <summary>
    /// שם הסשן
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// JSON של טאבים פתוחים
    /// </summary>
    public string TabsJson { get; set; } = "[]";

    /// <summary>
    /// האם שמירה אוטומטית
    /// </summary>
    public bool IsAutoSave { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// קישור בין טקסטים
/// </summary>
public class TextLink
{
    public int Id { get; set; }

    public int SourceContentId { get; set; }
    public Content SourceContent { get; set; } = null!;

    public int TargetContentId { get; set; }
    public Content TargetContent { get; set; } = null!;

    /// <summary>
    /// סוג הקישור: "similar", "reference", "continuation", "contrast", "source", "parallel"
    /// </summary>
    public string LinkType { get; set; } = "similar";

    /// <summary>
    /// ציון דמיון (0-1)
    /// </summary>
    public double? Similarity { get; set; }

    /// <summary>
    /// תיאור הקשר
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// האם נוצר ידנית
    /// </summary>
    public bool IsManual { get; set; }

    /// <summary>
    /// מי יצר: "system" או "user"
    /// </summary>
    public string CreatedBy { get; set; } = "system";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// הפניה למקור חיצוני (תנ"ך, גמרא)
/// </summary>
public class ExternalReference
{
    public int Id { get; set; }

    public int ContentId { get; set; }
    public Content Content { get; set; } = null!;

    /// <summary>
    /// סוג: "tanakh", "talmud", "midrash"
    /// </summary>
    public string RefType { get; set; } = string.Empty;

    /// <summary>
    /// ספר: "בראשית", "ברכות"
    /// </summary>
    public string RefBook { get; set; } = string.Empty;

    /// <summary>
    /// מיקום: "א:א", "דף ב."
    /// </summary>
    public string RefLocation { get; set; } = string.Empty;

    /// <summary>
    /// ציטוט (אופציונלי)
    /// </summary>
    public string? RefText { get; set; }

    /// <summary>
    /// מיקום בטקסט
    /// </summary>
    public int Position { get; set; }
}
