using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HolyBooks.Core.Models;
using HolyBooks.Data.Database;
using HolyBooks.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace HolyBooks.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly SefariaService _sefariaService;

    // Sub-ViewModels
    [ObservableProperty]
    private BookTreeViewModel _bookTree;

    [ObservableProperty]
    private ReaderViewModel _reader;

    [ObservableProperty]
    private SearchViewModel _search;

    [ObservableProperty]
    private TanakhViewModel? _tanakhViewModel;

    [ObservableProperty]
    private TalmudViewModel? _talmudViewModel;

    [ObservableProperty]
    private SourceSheetEditorViewModel? _sourceSheetEditorViewModel;

    [ObservableProperty]
    private SettingsViewModel? _settingsViewModel;

    // Navigation
    [ObservableProperty]
    private int _selectedNavigationIndex = 0;

    // UI State
    [ObservableProperty]
    private bool _isSearchPanelOpen;

    [ObservableProperty]
    private bool _isSidePanelOpen = true;

    [ObservableProperty]
    private string _currentTheme = "light";

    [ObservableProperty]
    private int _fontSize = 18;

    [ObservableProperty]
    private bool _isFullscreen;

    // Tabs for Rav Kook section
    public ObservableCollection<TabItemViewModel> OpenTabs { get; } = new();

    [ObservableProperty]
    private TabItemViewModel? _selectedTab;

    // Source Sheets
    public ObservableCollection<SourceSheetListItem> SourceSheets { get; } = new();

    [ObservableProperty]
    private SourceSheetListItem? _selectedSourceSheet;

    public MainWindowViewModel(
        AppDbContext db,
        SefariaService sefariaService,
        BookTreeViewModel bookTree,
        ReaderViewModel reader,
        SearchViewModel search,
        TanakhViewModel tanakhViewModel,
        TalmudViewModel talmudViewModel,
        SourceSheetEditorViewModel sourceSheetEditorViewModel,
        SettingsViewModel settingsViewModel)
    {
        _db = db;
        _sefariaService = sefariaService;
        _bookTree = bookTree;
        _reader = reader;
        _search = search;
        _tanakhViewModel = tanakhViewModel;
        _talmudViewModel = talmudViewModel;
        _sourceSheetEditorViewModel = sourceSheetEditorViewModel;
        _settingsViewModel = settingsViewModel;

        // Subscribe to book selection
        _bookTree.BookSelected += OnBookSelected;

        // Load source sheets
        _ = LoadSourceSheets();
    }

    private async Task LoadSourceSheets()
    {
        var sheets = await _db.SourceSheets
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new SourceSheetListItem
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                ItemCount = s.Items.Count
            })
            .ToListAsync();

        SourceSheets.Clear();
        foreach (var sheet in sheets)
        {
            SourceSheets.Add(sheet);
        }
    }

    partial void OnSelectedSourceSheetChanged(SourceSheetListItem? value)
    {
        if (value != null && SourceSheetEditorViewModel != null)
        {
            _ = SourceSheetEditorViewModel.LoadSheetAsync(value.Id);
        }
    }

    private void OnBookSelected(object? sender, ChapterSelectedEventArgs e)
    {
        OpenInNewTab(e.Chapter, e.Content);
    }

    [RelayCommand]
    private void OpenInNewTab(Chapter chapter, Content? content)
    {
        // Check if already open
        var existing = OpenTabs.FirstOrDefault(t => t.ChapterId == chapter.Id);
        if (existing != null)
        {
            SelectedTab = existing;
            return;
        }

        var tab = new TabItemViewModel
        {
            ChapterId = chapter.Id,
            Title = chapter.Title,
            Content = content?.TextContent ?? "",
            HtmlContent = content?.HtmlContent
        };

        OpenTabs.Add(tab);
        SelectedTab = tab;
    }

    [RelayCommand]
    private void CloseTab(TabItemViewModel tab)
    {
        var index = OpenTabs.IndexOf(tab);
        OpenTabs.Remove(tab);

        if (SelectedTab == tab && OpenTabs.Any())
        {
            SelectedTab = OpenTabs[Math.Max(0, index - 1)];
        }
    }

    [RelayCommand]
    private void CloseAllTabs()
    {
        OpenTabs.Clear();
        SelectedTab = null;
    }

    [RelayCommand]
    private void ToggleSearchPanel()
    {
        IsSearchPanelOpen = !IsSearchPanelOpen;
    }

    [RelayCommand]
    private void ToggleSidePanel()
    {
        IsSidePanelOpen = !IsSidePanelOpen;
    }

    [RelayCommand]
    private void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
        // Actual fullscreen toggle is handled by the Window
    }

    [RelayCommand]
    private void ChangeTheme(string theme)
    {
        CurrentTheme = theme;
        // TODO: Apply theme to application
    }

    [RelayCommand]
    private void IncreaseFontSize()
    {
        if (FontSize < 32)
            FontSize += 2;
    }

    [RelayCommand]
    private void DecreaseFontSize()
    {
        if (FontSize > 12)
            FontSize -= 2;
    }

    [RelayCommand]
    private async Task ImportBooks()
    {
        // TODO: Open import wizard window
    }

    // Navigation commands for Tanakh
    [RelayCommand]
    private async Task OpenTanakhBook(string bookName)
    {
        if (TanakhViewModel != null)
        {
            TanakhViewModel.SelectedBook = bookName;
            TanakhViewModel.SelectedChapter = 1;
            await TanakhViewModel.LoadChapter();
        }
    }

    // Navigation commands for Talmud
    [RelayCommand]
    private async Task OpenTalmudTractate(string tractate)
    {
        if (TalmudViewModel != null)
        {
            TalmudViewModel.CurrentTractate = tractate;
            TalmudViewModel.CurrentDaf = "2a";
            await TalmudViewModel.LoadPage();
        }
    }

    // Source Sheet commands
    [RelayCommand]
    private async Task CreateNewSourceSheet()
    {
        var sheet = new SourceSheet
        {
            Title = "דף מקורות חדש",
            Description = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.SourceSheets.Add(sheet);
        await _db.SaveChangesAsync();

        var item = new SourceSheetListItem
        {
            Id = sheet.Id,
            Title = sheet.Title,
            Description = sheet.Description,
            ItemCount = 0
        };

        SourceSheets.Insert(0, item);
        SelectedSourceSheet = item;

        // Switch to source sheets section
        SelectedNavigationIndex = 3;
    }

    [RelayCommand]
    private async Task DeleteSourceSheet(SourceSheetListItem sheet)
    {
        var entity = await _db.SourceSheets
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == sheet.Id);

        if (entity != null)
        {
            _db.SheetItems.RemoveRange(entity.Items);
            _db.SourceSheets.Remove(entity);
            await _db.SaveChangesAsync();

            SourceSheets.Remove(sheet);
        }
    }
}

public partial class TabItemViewModel : ObservableObject
{
    public int ChapterId { get; set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string? _htmlContent;

    [ObservableProperty]
    private double _scrollPosition;
}

public class ChapterSelectedEventArgs : EventArgs
{
    public Chapter Chapter { get; }
    public Content? Content { get; }

    public ChapterSelectedEventArgs(Chapter chapter, Content? content)
    {
        Chapter = chapter;
        Content = content;
    }
}

public class SourceSheetListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int ItemCount { get; set; }
}
