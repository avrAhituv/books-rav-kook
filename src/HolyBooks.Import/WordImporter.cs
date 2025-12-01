using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HolyBooks.Core.Models;
using System.Text.RegularExpressions;

namespace HolyBooks.Import;

/// <summary>
/// מייבא ספרים מקבצי Word
/// </summary>
public class WordImporter
{
    private readonly ImportSettings _settings;

    public WordImporter(ImportSettings? settings = null)
    {
        _settings = settings ?? new ImportSettings();
    }

    /// <summary>
    /// ייבוא קובץ Word לאובייקט ספר
    /// </summary>
    public ImportResult Import(string filePath)
    {
        var result = new ImportResult
        {
            SourceFile = filePath,
            ImportDate = DateTime.UtcNow
        };

        try
        {
            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;

            if (body == null)
            {
                result.Errors.Add("לא ניתן לקרוא את תוכן הקובץ");
                return result;
            }

            // Analyze document structure
            var elements = AnalyzeDocument(body);

            // Create book structure
            result.Book = CreateBookFromElements(elements, Path.GetFileNameWithoutExtension(filePath));
            result.Success = true;

            // Statistics
            result.Stats = new ImportStats
            {
                TotalChapters = CountChapters(result.Book),
                TotalSections = CountSections(result.Book),
                TotalWords = CountWords(result.Book)
            };
        }
        catch (Exception ex)
        {
            result.Errors.Add($"שגיאה בייבוא: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// ניתוח מסמך Word - זיהוי סגנונות או דפוסים
    /// </summary>
    private List<DocumentElement> AnalyzeDocument(Body body)
    {
        var elements = new List<DocumentElement>();

        foreach (var element in body.Elements())
        {
            if (element is Paragraph para)
            {
                var docElement = AnalyzeParagraph(para);
                if (docElement != null)
                {
                    elements.Add(docElement);
                }
            }
        }

        // If no styles detected, try heuristic detection
        if (!elements.Any(e => e.Type != ElementType.Content))
        {
            elements = ApplyHeuristicDetection(elements);
        }

        return elements;
    }

    /// <summary>
    /// ניתוח פסקה בודדת
    /// </summary>
    private DocumentElement? AnalyzeParagraph(Paragraph para)
    {
        var text = para.InnerText?.Trim();
        if (string.IsNullOrEmpty(text))
            return null;

        var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        var isBold = IsParagraphBold(para);

        // Try style-based detection first
        var type = DetectTypeByStyle(styleId);

        // If no style, try heuristic
        if (type == ElementType.Content)
        {
            type = DetectTypeByPattern(text, isBold);
        }

        return new DocumentElement
        {
            Text = text,
            Type = type,
            StyleId = styleId,
            IsBold = isBold,
            Level = GetLevelFromType(type)
        };
    }

    /// <summary>
    /// זיהוי לפי סגנון Word
    /// </summary>
    private ElementType DetectTypeByStyle(string? styleId)
    {
        if (string.IsNullOrEmpty(styleId))
            return ElementType.Content;

        return styleId.ToLower() switch
        {
            "heading1" or "1" => ElementType.BookTitle,
            "heading2" or "2" => ElementType.Chapter,
            "heading3" or "3" => ElementType.Section,
            "heading4" or "4" => ElementType.SubSection,
            "title" => ElementType.BookTitle,
            _ => ElementType.Content
        };
    }

    /// <summary>
    /// זיהוי לפי דפוסים (Heuristics)
    /// </summary>
    private ElementType DetectTypeByPattern(string text, bool isBold)
    {
        // Chapter patterns
        if (Regex.IsMatch(text, @"^פרק\s+[א-ת]+", RegexOptions.None))
            return ElementType.Chapter;

        if (Regex.IsMatch(text, @"^חלק\s+(ראשון|שני|שלישי|רביעי|[א-ת]+)", RegexOptions.None))
            return ElementType.Chapter;

        if (Regex.IsMatch(text, @"^שער\s+[א-ת]+", RegexOptions.None))
            return ElementType.Chapter;

        if (Regex.IsMatch(text, @"^מאמר\s+[א-ת]+", RegexOptions.None))
            return ElementType.Chapter;

        // Section patterns (א. ב. ג. or 1. 2. 3.)
        if (Regex.IsMatch(text, @"^[א-ת]\.", RegexOptions.None))
            return ElementType.Section;

        if (Regex.IsMatch(text, @"^\d+\.", RegexOptions.None))
            return ElementType.Section;

        if (Regex.IsMatch(text, @"^סעיף\s+[א-ת]", RegexOptions.None))
            return ElementType.Section;

        // Short bold text might be a heading
        if (isBold && text.Length < 100)
        {
            // Check if it looks like a title
            if (!text.Contains('.') || text.Length < 50)
                return ElementType.Chapter;
        }

        return ElementType.Content;
    }

    /// <summary>
    /// החלת זיהוי היוריסטי על כל המסמך
    /// </summary>
    private List<DocumentElement> ApplyHeuristicDetection(List<DocumentElement> elements)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            var elem = elements[i];

            // Short paragraph followed by longer content might be a heading
            if (elem.Text.Length < 80 && i < elements.Count - 1)
            {
                var next = elements[i + 1];
                if (next.Text.Length > 200)
                {
                    // Check if it looks like a title
                    if (elem.IsBold || !elem.Text.Contains("וכו'"))
                    {
                        elem.Type = ElementType.Chapter;
                    }
                }
            }
        }

        return elements;
    }

    /// <summary>
    /// בדיקה האם פסקה מודגשת
    /// </summary>
    private bool IsParagraphBold(Paragraph para)
    {
        var runs = para.Descendants<Run>();
        if (!runs.Any()) return false;

        return runs.All(r =>
            r.RunProperties?.Bold != null &&
            (r.RunProperties.Bold.Val == null || r.RunProperties.Bold.Val.Value));
    }

    /// <summary>
    /// יצירת אובייקט ספר מרשימת אלמנטים
    /// </summary>
    private Book CreateBookFromElements(List<DocumentElement> elements, string defaultTitle)
    {
        var book = new Book
        {
            Title = defaultTitle,
            Category = "rav_kook",
            Source = "local"
        };

        Chapter? currentChapter = null;
        Chapter? currentSection = null;
        var contentBuilder = new System.Text.StringBuilder();
        int chapterOrder = 0;
        int sectionOrder = 0;

        foreach (var elem in elements)
        {
            switch (elem.Type)
            {
                case ElementType.BookTitle:
                    book.Title = elem.Text;
                    break;

                case ElementType.Chapter:
                    // Save previous content
                    SaveContent(currentSection ?? currentChapter, contentBuilder);

                    currentChapter = new Chapter
                    {
                        Title = elem.Text,
                        Level = 1,
                        SortOrder = ++chapterOrder
                    };
                    book.Chapters.Add(currentChapter);
                    currentSection = null;
                    sectionOrder = 0;
                    break;

                case ElementType.Section:
                case ElementType.SubSection:
                    // Save previous content
                    SaveContent(currentSection ?? currentChapter, contentBuilder);

                    if (currentChapter == null)
                    {
                        // Create default chapter if none exists
                        currentChapter = new Chapter
                        {
                            Title = "פתיחה",
                            Level = 1,
                            SortOrder = ++chapterOrder
                        };
                        book.Chapters.Add(currentChapter);
                    }

                    currentSection = new Chapter
                    {
                        Title = elem.Text,
                        Level = elem.Type == ElementType.Section ? 2 : 3,
                        SortOrder = ++sectionOrder,
                        Parent = currentChapter
                    };
                    currentChapter.Children.Add(currentSection);
                    break;

                case ElementType.Content:
                    if (contentBuilder.Length > 0)
                        contentBuilder.AppendLine();
                    contentBuilder.Append(elem.Text);
                    break;
            }
        }

        // Save last content
        SaveContent(currentSection ?? currentChapter, contentBuilder);

        return book;
    }

    private void SaveContent(Chapter? chapter, System.Text.StringBuilder builder)
    {
        if (chapter == null || builder.Length == 0)
            return;

        var text = builder.ToString().Trim();
        chapter.Content = new Content
        {
            TextContent = text,
            WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
        };

        builder.Clear();
    }

    private int GetLevelFromType(ElementType type) => type switch
    {
        ElementType.BookTitle => 0,
        ElementType.Chapter => 1,
        ElementType.Section => 2,
        ElementType.SubSection => 3,
        _ => 4
    };

    private int CountChapters(Book book) =>
        book.Chapters.Count;

    private int CountSections(Book book) =>
        book.Chapters.Sum(c => c.Children.Count);

    private int CountWords(Book book)
    {
        int count = 0;
        foreach (var chapter in book.Chapters)
        {
            if (chapter.Content != null)
                count += chapter.Content.WordCount;
            foreach (var section in chapter.Children)
            {
                if (section.Content != null)
                    count += section.Content.WordCount;
            }
        }
        return count;
    }
}

/// <summary>
/// הגדרות ייבוא
/// </summary>
public class ImportSettings
{
    public bool UseStyles { get; set; } = true;
    public bool UseHeuristics { get; set; } = true;
    public string? ChapterPattern { get; set; }
    public string? SectionPattern { get; set; }
    public bool BoldAsSection { get; set; } = true;
}

/// <summary>
/// תוצאת ייבוא
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public Book? Book { get; set; }
    public string SourceFile { get; set; } = string.Empty;
    public DateTime ImportDate { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public ImportStats? Stats { get; set; }
}

/// <summary>
/// סטטיסטיקות ייבוא
/// </summary>
public class ImportStats
{
    public int TotalChapters { get; set; }
    public int TotalSections { get; set; }
    public int TotalWords { get; set; }
}

/// <summary>
/// אלמנט במסמך
/// </summary>
internal class DocumentElement
{
    public string Text { get; set; } = string.Empty;
    public ElementType Type { get; set; }
    public string? StyleId { get; set; }
    public bool IsBold { get; set; }
    public int Level { get; set; }
}

/// <summary>
/// סוג אלמנט
/// </summary>
internal enum ElementType
{
    BookTitle,
    Chapter,
    Section,
    SubSection,
    Content
}
