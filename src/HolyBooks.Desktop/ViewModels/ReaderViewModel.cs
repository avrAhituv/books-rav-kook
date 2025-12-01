using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HolyBooks.Core.Models;
using HolyBooks.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace HolyBooks.Desktop.ViewModels;

public partial class ReaderViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    [ObservableProperty]
    private Chapter? _currentChapter;

    [ObservableProperty]
    private Content? _currentContent;

    [ObservableProperty]
    private string _displayText = string.Empty;

    [ObservableProperty]
    private int _fontSize = 18;

    [ObservableProperty]
    private double _lineHeight = 1.8;

    [ObservableProperty]
    private string _fontFamily = "David";

    [ObservableProperty]
    private bool _showNotes = true;

    [ObservableProperty]
    private bool _showLinks = true;

    public ReaderViewModel(AppDbContext db)
    {
        _db = db;
    }

    [RelayCommand]
    public async Task LoadContent(int chapterId)
    {
        CurrentChapter = await _db.Chapters
            .Include(c => c.Content)
            .Include(c => c.Book)
            .FirstOrDefaultAsync(c => c.Id == chapterId);

        CurrentContent = CurrentChapter?.Content;
        DisplayText = CurrentContent?.TextContent ?? "";
    }

    [RelayCommand]
    public async Task NavigateToNext()
    {
        if (CurrentChapter == null) return;

        var nextChapter = await _db.Chapters
            .Where(c => c.BookId == CurrentChapter.BookId
                     && c.SortOrder > CurrentChapter.SortOrder)
            .OrderBy(c => c.SortOrder)
            .FirstOrDefaultAsync();

        if (nextChapter != null)
        {
            await LoadContent(nextChapter.Id);
        }
    }

    [RelayCommand]
    public async Task NavigateToPrevious()
    {
        if (CurrentChapter == null) return;

        var prevChapter = await _db.Chapters
            .Where(c => c.BookId == CurrentChapter.BookId
                     && c.SortOrder < CurrentChapter.SortOrder)
            .OrderByDescending(c => c.SortOrder)
            .FirstOrDefaultAsync();

        if (prevChapter != null)
        {
            await LoadContent(prevChapter.Id);
        }
    }

    [RelayCommand]
    public async Task AddBookmark(int position, string? title = null)
    {
        if (CurrentContent == null) return;

        var bookmark = new Bookmark
        {
            ContentId = CurrentContent.Id,
            Position = position,
            Title = title ?? $"סימניה - {CurrentChapter?.Title}"
        };

        _db.Bookmarks.Add(bookmark);
        await _db.SaveChangesAsync();
    }

    [RelayCommand]
    public async Task AddNote(int startPosition, int endPosition, string noteText, string color = "#FFFF00")
    {
        if (CurrentContent == null) return;

        var note = new Note
        {
            ContentId = CurrentContent.Id,
            StartPosition = startPosition,
            EndPosition = endPosition,
            NoteText = noteText,
            NoteType = string.IsNullOrEmpty(noteText) ? "highlight" : "note",
            Color = color
        };

        _db.Notes.Add(note);
        await _db.SaveChangesAsync();
    }

    [RelayCommand]
    public async Task SaveReadingPosition(double scrollPosition)
    {
        if (CurrentContent == null) return;

        var history = await _db.ReadingHistory
            .FirstOrDefaultAsync(h => h.ContentId == CurrentContent.Id);

        if (history == null)
        {
            history = new ReadingHistory { ContentId = CurrentContent.Id };
            _db.ReadingHistory.Add(history);
        }

        history.ScrollPosition = scrollPosition;
        history.LastRead = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    [RelayCommand]
    public void CopyWithSource()
    {
        // TODO: Copy selected text with source reference
        var source = $"{CurrentChapter?.Book?.Title}, {CurrentChapter?.Title}";
        // Clipboard.SetTextAsync($"{selectedText}\n({source})");
    }
}
