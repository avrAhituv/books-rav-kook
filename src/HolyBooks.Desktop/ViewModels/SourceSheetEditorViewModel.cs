using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HolyBooks.Core.Models;
using HolyBooks.Data.Database;
using HolyBooks.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace HolyBooks.Desktop.ViewModels;

/// <summary>
/// ViewModel לעורך דפי מקורות
/// </summary>
public partial class SourceSheetEditorViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly SefariaService _sefaria;

    [ObservableProperty]
    private int? _sheetId;

    [ObservableProperty]
    private string _title = "דף מקורות חדש";

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isSaving;

    // Items in the sheet
    public ObservableCollection<SheetItemViewModel> Items { get; } = new();

    // Selected item
    [ObservableProperty]
    private SheetItemViewModel? _selectedItem;

    // Search
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    public ObservableCollection<SourceSearchResult> SearchResults { get; } = new();

    // Tags
    public ObservableCollection<string> Tags { get; } = new();

    [ObservableProperty]
    private string _newTag = string.Empty;

    public SourceSheetEditorViewModel(AppDbContext db, SefariaService sefaria)
    {
        _db = db;
        _sefaria = sefaria;

        Items.CollectionChanged += (_, _) => IsDirty = true;
    }

    public async Task LoadSheetAsync(int sheetId) => await LoadSheet(sheetId);

    [RelayCommand]
    public async Task LoadSheet(int sheetId)
    {
        var sheet = await _db.SourceSheets
            .Include(s => s.Items.OrderBy(i => i.SortOrder))
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(s => s.Id == sheetId);

        if (sheet == null) return;

        SheetId = sheet.Id;
        Title = sheet.Title;
        Description = sheet.Description ?? "";

        Items.Clear();
        foreach (var item in sheet.Items)
        {
            Items.Add(new SheetItemViewModel
            {
                Id = item.Id,
                ItemType = item.ItemType,
                SourceRef = item.SourceRef,
                SourceText = item.SourceText ?? "",
                CustomText = item.CustomText ?? "",
                Note = item.Note ?? "",
                SortOrder = item.SortOrder
            });
        }

        Tags.Clear();
        foreach (var tag in sheet.Tags)
        {
            Tags.Add(tag.Tag);
        }

        IsDirty = false;
    }

    [RelayCommand]
    public async Task Save()
    {
        IsSaving = true;

        try
        {
            SourceSheet sheet;

            if (SheetId.HasValue)
            {
                sheet = await _db.SourceSheets
                    .Include(s => s.Items)
                    .Include(s => s.Tags)
                    .FirstAsync(s => s.Id == SheetId);

                sheet.Title = Title;
                sheet.Description = Description;
                sheet.UpdatedAt = DateTime.UtcNow;

                // Remove old items
                _db.SheetItems.RemoveRange(sheet.Items);
                _db.SheetTags.RemoveRange(sheet.Tags);
            }
            else
            {
                sheet = new SourceSheet
                {
                    Title = Title,
                    Description = Description
                };
                _db.SourceSheets.Add(sheet);
            }

            // Add items
            int order = 0;
            foreach (var item in Items)
            {
                sheet.Items.Add(new SheetItem
                {
                    ItemType = item.ItemType,
                    SourceRef = item.SourceRef,
                    SourceText = item.SourceText,
                    CustomText = item.CustomText,
                    Note = item.Note,
                    SortOrder = order++
                });
            }

            // Add tags
            foreach (var tag in Tags)
            {
                sheet.Tags.Add(new SheetTag { Tag = tag });
            }

            await _db.SaveChangesAsync();
            SheetId = sheet.Id;
            IsDirty = false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    #region Add Items

    [RelayCommand]
    public void AddHeading()
    {
        var item = new SheetItemViewModel
        {
            ItemType = "heading",
            CustomText = "כותרת חדשה",
            SortOrder = Items.Count
        };
        Items.Add(item);
        SelectedItem = item;
    }

    [RelayCommand]
    public void AddText()
    {
        var item = new SheetItemViewModel
        {
            ItemType = "text",
            CustomText = "",
            SortOrder = Items.Count
        };
        Items.Add(item);
        SelectedItem = item;
    }

    [RelayCommand]
    public void AddDivider()
    {
        Items.Add(new SheetItemViewModel
        {
            ItemType = "divider",
            SortOrder = Items.Count
        });
    }

    [RelayCommand]
    public void AddSourceFromSearch(SourceSearchResult result)
    {
        var item = new SheetItemViewModel
        {
            ItemType = "source",
            SourceRef = result.Reference,
            SourceText = result.Text,
            SourceType = result.SourceType,
            SortOrder = Items.Count
        };
        Items.Add(item);
        SelectedItem = item;
    }

    #endregion

    #region Item Management

    [RelayCommand]
    public void RemoveItem(SheetItemViewModel item)
    {
        Items.Remove(item);
        ReorderItems();
    }

    [RelayCommand]
    public void MoveItemUp(SheetItemViewModel item)
    {
        var index = Items.IndexOf(item);
        if (index > 0)
        {
            Items.Move(index, index - 1);
            ReorderItems();
        }
    }

    [RelayCommand]
    public void MoveItemDown(SheetItemViewModel item)
    {
        var index = Items.IndexOf(item);
        if (index < Items.Count - 1)
        {
            Items.Move(index, index + 1);
            ReorderItems();
        }
    }

    [RelayCommand]
    public void DuplicateItem(SheetItemViewModel item)
    {
        var copy = new SheetItemViewModel
        {
            ItemType = item.ItemType,
            SourceRef = item.SourceRef,
            SourceText = item.SourceText,
            SourceType = item.SourceType,
            CustomText = item.CustomText,
            Note = item.Note,
            SortOrder = Items.Count
        };

        var index = Items.IndexOf(item);
        Items.Insert(index + 1, copy);
        ReorderItems();
    }

    private void ReorderItems()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].SortOrder = i;
        }
    }

    #endregion

    #region Search

    [RelayCommand]
    public async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        IsSearching = true;
        SearchResults.Clear();

        try
        {
            // Search in local books
            var localResults = await _db.Contents
                .Include(c => c.Chapter)
                    .ThenInclude(ch => ch.Book)
                .Where(c => EF.Functions.Like(c.TextContent, $"%{SearchQuery}%"))
                .Take(20)
                .ToListAsync();

            foreach (var result in localResults)
            {
                SearchResults.Add(new SourceSearchResult
                {
                    Reference = $"{result.Chapter.Book.Title}:{result.Chapter.Title}",
                    HebrewRef = $"{result.Chapter.Book.Title}, {result.Chapter.Title}",
                    Text = ExtractSnippet(result.TextContent, SearchQuery),
                    SourceType = "local",
                    ContentId = result.Id
                });
            }

            // Search in Sefaria
            var sefariaResults = await _sefaria.SearchAsync(SearchQuery, limit: 20);
            if (sefariaResults?.Hits?.Hits != null)
            {
                foreach (var hit in sefariaResults.Hits.Hits)
                {
                    if (hit.Source != null)
                    {
                        SearchResults.Add(new SourceSearchResult
                        {
                            Reference = hit.Source.Ref ?? "",
                            HebrewRef = hit.Source.HeRef ?? hit.Source.Ref ?? "",
                            Text = hit.Source.Content ?? "",
                            SourceType = "sefaria"
                        });
                    }
                }
            }
        }
        finally
        {
            IsSearching = false;
        }
    }

    private string ExtractSnippet(string text, string query, int length = 150)
    {
        var index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return text.Length > length ? text[..length] + "..." : text;

        var start = Math.Max(0, index - 50);
        var end = Math.Min(text.Length, index + query.Length + 100);
        var snippet = text[start..end];

        if (start > 0) snippet = "..." + snippet;
        if (end < text.Length) snippet += "...";

        return snippet;
    }

    #endregion

    #region Tags

    [RelayCommand]
    public void AddTag()
    {
        if (!string.IsNullOrWhiteSpace(NewTag) && !Tags.Contains(NewTag))
        {
            Tags.Add(NewTag);
            NewTag = string.Empty;
            IsDirty = true;
        }
    }

    [RelayCommand]
    public void RemoveTag(string tag)
    {
        Tags.Remove(tag);
        IsDirty = true;
    }

    #endregion
}

public partial class SheetItemViewModel : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _itemType = "source";

    [ObservableProperty]
    private string? _sourceRef;

    [ObservableProperty]
    private string _sourceText = string.Empty;

    [ObservableProperty]
    private string? _sourceType;

    [ObservableProperty]
    private string _customText = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private int _sortOrder;

    public string DisplayText => ItemType switch
    {
        "heading" => CustomText,
        "text" => CustomText.Length > 50 ? CustomText[..50] + "..." : CustomText,
        "source" => SourceText.Length > 50 ? SourceText[..50] + "..." : SourceText,
        "divider" => "─────────",
        _ => ""
    };

    public string Icon => ItemType switch
    {
        "heading" => "📌",
        "text" => "📝",
        "source" => "📖",
        "divider" => "➖",
        _ => "•"
    };
}

public class SourceSearchResult
{
    public string Reference { get; set; } = string.Empty;
    public string HebrewRef { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty; // "local" or "sefaria"
    public int? ContentId { get; set; }
}
