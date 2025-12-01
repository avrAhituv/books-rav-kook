using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HolyBooks.Core.Models;
using HolyBooks.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace HolyBooks.Desktop.ViewModels;

public partial class BookTreeViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    public ObservableCollection<BookTreeNode> RootNodes { get; } = new();

    [ObservableProperty]
    private BookTreeNode? _selectedNode;

    [ObservableProperty]
    private string _filterText = string.Empty;

    public event EventHandler<ChapterSelectedEventArgs>? BookSelected;

    public BookTreeViewModel(AppDbContext db)
    {
        _db = db;
        LoadBooks();
    }

    private async void LoadBooks()
    {
        var books = await _db.Books
            .Include(b => b.Chapters.Where(c => c.ParentId == null))
            .OrderBy(b => b.Category)
            .ThenBy(b => b.SortOrder)
            .ToListAsync();

        // Group by category
        var categories = books.GroupBy(b => b.Category);

        foreach (var category in categories)
        {
            var categoryNode = new BookTreeNode
            {
                Title = GetCategoryTitle(category.Key),
                NodeType = TreeNodeType.Category,
                IsExpanded = true
            };

            foreach (var book in category)
            {
                var bookNode = new BookTreeNode
                {
                    Title = book.Title,
                    BookId = book.Id,
                    NodeType = TreeNodeType.Book
                };

                // Load top-level chapters
                foreach (var chapter in book.Chapters.OrderBy(c => c.SortOrder))
                {
                    bookNode.Children.Add(CreateChapterNode(chapter));
                }

                categoryNode.Children.Add(bookNode);
            }

            RootNodes.Add(categoryNode);
        }
    }

    private BookTreeNode CreateChapterNode(Chapter chapter)
    {
        var node = new BookTreeNode
        {
            Title = chapter.Title,
            ChapterId = chapter.Id,
            NodeType = TreeNodeType.Chapter,
            Level = chapter.Level
        };

        // Children will be loaded on expand
        if (chapter.Children.Any())
        {
            foreach (var child in chapter.Children.OrderBy(c => c.SortOrder))
            {
                node.Children.Add(CreateChapterNode(child));
            }
        }

        return node;
    }

    private string GetCategoryTitle(string category) => category switch
    {
        "rav_kook" => "כתבי הרב קוק",
        "tanakh" => "תנ\"ך",
        "talmud" => "תלמוד בבלי",
        "commentary" => "מפרשים",
        _ => category
    };

    partial void OnSelectedNodeChanged(BookTreeNode? value)
    {
        if (value?.ChapterId != null)
        {
            SelectChapter(value.ChapterId.Value);
        }
    }

    [RelayCommand]
    private async Task SelectChapter(int chapterId)
    {
        var chapter = await _db.Chapters
            .Include(c => c.Content)
            .FirstOrDefaultAsync(c => c.Id == chapterId);

        if (chapter != null)
        {
            BookSelected?.Invoke(this, new ChapterSelectedEventArgs(chapter, chapter.Content));
        }
    }

    [RelayCommand]
    private async Task LoadChildren(BookTreeNode node)
    {
        if (node.ChildrenLoaded || node.ChapterId == null)
            return;

        var children = await _db.Chapters
            .Where(c => c.ParentId == node.ChapterId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        node.Children.Clear();
        foreach (var child in children)
        {
            node.Children.Add(CreateChapterNode(child));
        }

        node.ChildrenLoaded = true;
    }

    partial void OnFilterTextChanged(string value)
    {
        // TODO: Filter tree nodes
    }
}

public partial class BookTreeNode : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    public int? BookId { get; set; }
    public int? ChapterId { get; set; }
    public TreeNodeType NodeType { get; set; }
    public int Level { get; set; }
    public bool ChildrenLoaded { get; set; }

    public ObservableCollection<BookTreeNode> Children { get; } = new();

    public string Icon => NodeType switch
    {
        TreeNodeType.Category => "📚",
        TreeNodeType.Book => "📖",
        TreeNodeType.Chapter => Level switch
        {
            1 => "📄",
            2 => "📝",
            _ => "•"
        },
        _ => ""
    };
}

public enum TreeNodeType
{
    Category,
    Book,
    Chapter
}
