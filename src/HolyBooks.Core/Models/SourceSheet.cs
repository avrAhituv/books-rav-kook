namespace HolyBooks.Core.Models;

/// <summary>
/// דף מקורות
/// </summary>
public class SourceSheet
{
    public int Id { get; set; }

    /// <summary>
    /// כותרת דף המקורות
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// תיאור
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// האם זו תבנית
    /// </summary>
    public bool IsTemplate { get; set; }

    /// <summary>
    /// תיקייה
    /// </summary>
    public int? FolderId { get; set; }
    public SheetFolder? Folder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<SheetItem> Items { get; set; } = new List<SheetItem>();
    public ICollection<SheetTag> Tags { get; set; } = new List<SheetTag>();
}

/// <summary>
/// תיקיית דפי מקורות
/// </summary>
public class SheetFolder
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? ParentId { get; set; }
    public SheetFolder? Parent { get; set; }

    public int SortOrder { get; set; }

    public ICollection<SheetFolder> Children { get; set; } = new List<SheetFolder>();
    public ICollection<SourceSheet> Sheets { get; set; } = new List<SourceSheet>();
}

/// <summary>
/// פריט בדף מקורות
/// </summary>
public class SheetItem
{
    public int Id { get; set; }

    public int SheetId { get; set; }
    public SourceSheet Sheet { get; set; } = null!;

    /// <summary>
    /// סוג: "source", "text", "heading", "divider", "image"
    /// </summary>
    public string ItemType { get; set; } = "source";

    /// <summary>
    /// סדר
    /// </summary>
    public int SortOrder { get; set; }

    // עבור מקורות
    /// <summary>
    /// הפניה: "Genesis.1.1" או content_id
    /// </summary>
    public string? SourceRef { get; set; }

    /// <summary>
    /// סוג מקור: "sefaria", "local", "bookmark"
    /// </summary>
    public string? SourceType { get; set; }

    /// <summary>
    /// טקסט המקור (cached)
    /// </summary>
    public string? SourceText { get; set; }

    // עבור טקסט חופשי/כותרת
    /// <summary>
    /// טקסט מותאם אישית
    /// </summary>
    public string? CustomText { get; set; }

    /// <summary>
    /// הערה למקור
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// עיצוב (JSON)
    /// </summary>
    public string? StyleJson { get; set; }
}

/// <summary>
/// תגית לדף מקורות
/// </summary>
public class SheetTag
{
    public int Id { get; set; }

    public int SheetId { get; set; }
    public SourceSheet Sheet { get; set; } = null!;

    public string Tag { get; set; } = string.Empty;
}
