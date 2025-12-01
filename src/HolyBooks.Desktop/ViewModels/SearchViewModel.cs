using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HolyBooks.Core.Models;
using HolyBooks.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace HolyBooks.Desktop.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private int _resultCount;

    [ObservableProperty]
    private double _searchTime;

    public ObservableCollection<SearchResultItem> Results { get; } = new();

    // Filters
    [ObservableProperty]
    private bool _searchInRavKook = true;

    [ObservableProperty]
    private bool _searchInTanakh = true;

    [ObservableProperty]
    private bool _searchInTalmud = true;

    [ObservableProperty]
    private string _sortBy = "relevance"; // "relevance", "book", "chapter"

    public SearchViewModel(AppDbContext db)
    {
        _db = db;
    }

    [RelayCommand]
    public async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        IsSearching = true;
        Results.Clear();

        var startTime = DateTime.Now;

        try
        {
            // Build category filter
            var categories = new List<string>();
            if (SearchInRavKook) categories.Add("rav_kook");
            if (SearchInTanakh) categories.Add("tanakh");
            if (SearchInTalmud) categories.Add("talmud");

            // Simple LIKE search (will be replaced with Lucene)
            var results = await _db.Contents
                .Include(c => c.Chapter)
                    .ThenInclude(ch => ch.Book)
                .Where(c => categories.Contains(c.Chapter.Book.Category))
                .Where(c => EF.Functions.Like(c.TextContent, $"%{SearchQuery}%"))
                .Take(100)
                .ToListAsync();

            foreach (var content in results)
            {
                var snippet = ExtractSnippet(content.TextContent, SearchQuery);

                Results.Add(new SearchResultItem
                {
                    ContentId = content.Id,
                    BookTitle = content.Chapter.Book.Title,
                    ChapterTitle = content.Chapter.Title,
                    Snippet = snippet,
                    HighlightedSnippet = HighlightQuery(snippet, SearchQuery)
                });
            }

            ResultCount = Results.Count;
            SearchTime = (DateTime.Now - startTime).TotalSeconds;
        }
        finally
        {
            IsSearching = false;
        }
    }

    private string ExtractSnippet(string text, string query, int contextLength = 100)
    {
        var index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return text.Length > 200 ? text[..200] + "..." : text;

        var start = Math.Max(0, index - contextLength);
        var end = Math.Min(text.Length, index + query.Length + contextLength);

        var snippet = text[start..end];

        if (start > 0) snippet = "..." + snippet;
        if (end < text.Length) snippet += "...";

        return snippet;
    }

    private string HighlightQuery(string text, string query)
    {
        // For display - will wrap matches in ** for bold
        return text.Replace(query, $"**{query}**", StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchQuery = string.Empty;
        Results.Clear();
        ResultCount = 0;
    }
}

public class SearchResultItem
{
    public int ContentId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string ChapterTitle { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public string HighlightedSnippet { get; set; } = string.Empty;

    public string FullReference => $"{BookTitle} > {ChapterTitle}";
}
