using HolyBooks.Core.Models;
using HolyBooks.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace HolyBooks.Import;

/// <summary>
/// שירות ייבוא ספרים למאגר
/// </summary>
public class ImportService
{
    private readonly AppDbContext _db;
    private readonly WordImporter _wordImporter;

    public ImportService(AppDbContext db)
    {
        _db = db;
        _wordImporter = new WordImporter();
    }

    /// <summary>
    /// ייבוא קובץ Word בודד
    /// </summary>
    public async Task<ImportResult> ImportFileAsync(string filePath, ImportSettings? settings = null)
    {
        var importer = settings != null ? new WordImporter(settings) : _wordImporter;
        var result = importer.Import(filePath);

        if (result.Success && result.Book != null)
        {
            await SaveBookAsync(result.Book);
        }

        return result;
    }

    /// <summary>
    /// ייבוא תיקייה שלמה
    /// </summary>
    public async Task<List<ImportResult>> ImportFolderAsync(string folderPath, ImportSettings? settings = null)
    {
        var results = new List<ImportResult>();

        var files = Directory.GetFiles(folderPath, "*.docx")
            .Concat(Directory.GetFiles(folderPath, "*.doc"));

        foreach (var file in files)
        {
            var result = await ImportFileAsync(file, settings);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// שמירת ספר למסד הנתונים
    /// </summary>
    private async Task SaveBookAsync(Book book)
    {
        // Check if book already exists
        var existing = await _db.Books
            .FirstOrDefaultAsync(b => b.Title == book.Title);

        if (existing != null)
        {
            // Update existing book
            existing.Description = book.Description;
            existing.Author = book.Author;

            // Remove old chapters
            var oldChapters = await _db.Chapters
                .Where(c => c.BookId == existing.Id)
                .ToListAsync();
            _db.Chapters.RemoveRange(oldChapters);

            // Add new chapters
            foreach (var chapter in book.Chapters)
            {
                chapter.BookId = existing.Id;
                _db.Chapters.Add(chapter);
            }
        }
        else
        {
            // Add new book
            _db.Books.Add(book);
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// קבלת תצוגה מקדימה של הייבוא (ללא שמירה)
    /// </summary>
    public ImportResult PreviewImport(string filePath, ImportSettings? settings = null)
    {
        var importer = settings != null ? new WordImporter(settings) : _wordImporter;
        return importer.Import(filePath);
    }

    /// <summary>
    /// בדיקת קובץ לפני ייבוא
    /// </summary>
    public FileAnalysis AnalyzeFile(string filePath)
    {
        var analysis = new FileAnalysis
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath)
        };

        try
        {
            var fi = new FileInfo(filePath);
            analysis.FileSize = fi.Length;
            analysis.LastModified = fi.LastWriteTime;

            // Try to detect if it has styles
            var preview = PreviewImport(filePath);
            if (preview.Success && preview.Book != null)
            {
                analysis.HasStyles = preview.Book.Chapters.Any();
                analysis.DetectedChapters = preview.Stats?.TotalChapters ?? 0;
                analysis.DetectedSections = preview.Stats?.TotalSections ?? 0;
                analysis.TotalWords = preview.Stats?.TotalWords ?? 0;
                analysis.Status = analysis.HasStyles ? FileStatus.Ready : FileStatus.NeedsReview;
            }
            else
            {
                analysis.Status = FileStatus.Error;
                analysis.ErrorMessage = preview.Errors.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            analysis.Status = FileStatus.Error;
            analysis.ErrorMessage = ex.Message;
        }

        return analysis;
    }
}

/// <summary>
/// ניתוח קובץ
/// </summary>
public class FileAnalysis
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public bool HasStyles { get; set; }
    public int DetectedChapters { get; set; }
    public int DetectedSections { get; set; }
    public int TotalWords { get; set; }
    public FileStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum FileStatus
{
    Pending,
    Ready,
    NeedsReview,
    Importing,
    Done,
    Error
}
