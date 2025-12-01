namespace HolyBooks.Core.Models;

/// <summary>
/// ספר במאגר
/// </summary>
public class Book
{
    public int Id { get; set; }

    /// <summary>
    /// שם הספר בעברית
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// שם הספר באנגלית (לחיפוש ו-API)
    /// </summary>
    public string? TitleEnglish { get; set; }

    /// <summary>
    /// קטגוריה: "rav_kook", "tanakh", "talmud", "commentary"
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// תת-קטגוריה: "machshava", "halacha", "drush", "torah", "neviim", "ketuvim"
    /// </summary>
    public string? SubCategory { get; set; }

    /// <summary>
    /// מחבר הספר
    /// </summary>
    public string Author { get; set; } = "הרב אברהם יצחק הכהן קוק";

    /// <summary>
    /// שנת פרסום
    /// </summary>
    public int? PublicationYear { get; set; }

    /// <summary>
    /// תיאור קצר
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// נתיב לתמונת כריכה
    /// </summary>
    public string? CoverImage { get; set; }

    /// <summary>
    /// סדר תצוגה
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// מקור: "local" או "sefaria"
    /// </summary>
    public string Source { get; set; } = "local";

    /// <summary>
    /// מזהה בספריא (אם רלוונטי)
    /// </summary>
    public string? SefariaRef { get; set; }

    /// <summary>
    /// תאריך ייבוא
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}

/// <summary>
/// פרק/סעיף בספר (היררכי)
/// </summary>
public class Chapter
{
    public int Id { get; set; }

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    /// <summary>
    /// פרק אב (להיררכיה)
    /// </summary>
    public int? ParentId { get; set; }
    public Chapter? Parent { get; set; }

    /// <summary>
    /// כותרת הפרק
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// רמה בהיררכיה: 1=פרק, 2=סעיף, 3=תת-סעיף
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// סדר תצוגה
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// נתיב להיררכיה: "1.2.3"
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// מזהה בספריא
    /// </summary>
    public string? SefariaRef { get; set; }

    // Navigation
    public ICollection<Chapter> Children { get; set; } = new List<Chapter>();
    public Content? Content { get; set; }
}

/// <summary>
/// תוכן טקסט
/// </summary>
public class Content
{
    public int Id { get; set; }

    public int ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    /// <summary>
    /// הטקסט הגולמי
    /// </summary>
    public string TextContent { get; set; } = string.Empty;

    /// <summary>
    /// טקסט עם HTML לעיצוב
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// מספר מילים
    /// </summary>
    public int WordCount { get; set; }

    // Navigation
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
    public ICollection<TextLink> SourceLinks { get; set; } = new List<TextLink>();
    public ICollection<TextLink> TargetLinks { get; set; } = new List<TextLink>();
}
