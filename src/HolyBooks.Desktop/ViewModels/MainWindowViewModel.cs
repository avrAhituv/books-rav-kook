using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HolyBooks.Core.Models;

namespace HolyBooks.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private BookTreeViewModel _bookTree;

    [ObservableProperty]
    private ReaderViewModel _reader;

    [ObservableProperty]
    private SearchViewModel _search;

    [ObservableProperty]
    private bool _isSearchPanelOpen;

    [ObservableProperty]
    private bool _isSidePanelOpen = true;

    [ObservableProperty]
    private string _currentTheme = "light";

    [ObservableProperty]
    private int _fontSize = 18;

    public ObservableCollection<TabItemViewModel> OpenTabs { get; } = new();

    [ObservableProperty]
    private TabItemViewModel? _selectedTab;

    public MainWindowViewModel(BookTreeViewModel bookTree, ReaderViewModel reader, SearchViewModel search)
    {
        _bookTree = bookTree;
        _reader = reader;
        _search = search;

        // Subscribe to book selection
        _bookTree.BookSelected += OnBookSelected;
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
    private void ChangeTheme(string theme)
    {
        CurrentTheme = theme;
        // TODO: Apply theme
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
        // TODO: Open import wizard
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
