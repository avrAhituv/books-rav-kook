using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using HolyBooks.Core.Models;

namespace HolyBooks.Data.Search;

/// <summary>
/// שירות חיפוש Full-Text עם Lucene.NET
/// </summary>
public class LuceneSearchService : IDisposable
{
    private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

    private readonly string _indexPath;
    private readonly Analyzer _analyzer;
    private FSDirectory? _directory;
    private IndexWriter? _writer;

    public LuceneSearchService(string? indexPath = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _indexPath = indexPath ?? Path.Combine(appData, "HolyBooks", "search_index");
        System.IO.Directory.CreateDirectory(_indexPath);

        _analyzer = new StandardAnalyzer(AppLuceneVersion);
    }

    /// <summary>
    /// אינדוקס תוכן
    /// </summary>
    public void IndexContent(Content content, string bookTitle, string chapterTitle)
    {
        EnsureWriter();

        var doc = new Document
        {
            new StringField("id", content.Id.ToString(), Field.Store.YES),
            new TextField("book", bookTitle, Field.Store.YES),
            new TextField("chapter", chapterTitle, Field.Store.YES),
            new TextField("content", content.TextContent, Field.Store.YES),
            new Int32Field("content_id", content.Id, Field.Store.YES)
        };

        _writer!.UpdateDocument(new Term("id", content.Id.ToString()), doc);
    }

    /// <summary>
    /// אינדוקס ספר שלם
    /// </summary>
    public void IndexBook(Book book)
    {
        EnsureWriter();

        foreach (var chapter in book.Chapters)
        {
            IndexChapter(chapter, book.Title);
        }

        _writer!.Commit();
    }

    private void IndexChapter(Chapter chapter, string bookTitle)
    {
        if (chapter.Content != null)
        {
            IndexContent(chapter.Content, bookTitle, chapter.Title);
        }

        foreach (var child in chapter.Children)
        {
            IndexChapter(child, bookTitle);
        }
    }

    /// <summary>
    /// חיפוש
    /// </summary>
    public List<SearchResult> Search(string queryText, int maxResults = 100, string[]? categories = null)
    {
        var results = new List<SearchResult>();

        using var directory = FSDirectory.Open(_indexPath);
        using var reader = DirectoryReader.Open(directory);
        var searcher = new IndexSearcher(reader);

        var parser = new MultiFieldQueryParser(
            AppLuceneVersion,
            new[] { "content", "chapter", "book" },
            _analyzer);

        try
        {
            var query = parser.Parse(EscapeQuery(queryText));

            // Add category filter if specified
            if (categories != null && categories.Any())
            {
                var boolQuery = new BooleanQuery
                {
                    { query, Occur.MUST }
                };

                var categoryQuery = new BooleanQuery();
                foreach (var cat in categories)
                {
                    categoryQuery.Add(new TermQuery(new Term("category", cat)), Occur.SHOULD);
                }
                boolQuery.Add(categoryQuery, Occur.MUST);

                query = boolQuery;
            }

            var hits = searcher.Search(query, maxResults);

            foreach (var hit in hits.ScoreDocs)
            {
                var doc = searcher.Doc(hit.Doc);
                results.Add(new SearchResult
                {
                    ContentId = int.Parse(doc.Get("content_id")),
                    BookTitle = doc.Get("book"),
                    ChapterTitle = doc.Get("chapter"),
                    Content = doc.Get("content"),
                    Score = hit.Score
                });
            }
        }
        catch (ParseException)
        {
            // If query parsing fails, try a simple term query
            var termQuery = new TermQuery(new Term("content", queryText.ToLower()));
            var hits = searcher.Search(termQuery, maxResults);

            foreach (var hit in hits.ScoreDocs)
            {
                var doc = searcher.Doc(hit.Doc);
                results.Add(new SearchResult
                {
                    ContentId = int.Parse(doc.Get("content_id")),
                    BookTitle = doc.Get("book"),
                    ChapterTitle = doc.Get("chapter"),
                    Content = doc.Get("content"),
                    Score = hit.Score
                });
            }
        }

        return results;
    }

    /// <summary>
    /// חיפוש עם הדגשה
    /// </summary>
    public List<SearchResult> SearchWithHighlight(string queryText, int maxResults = 100)
    {
        var results = Search(queryText, maxResults);

        foreach (var result in results)
        {
            result.Snippet = ExtractSnippet(result.Content, queryText);
            result.HighlightedSnippet = HighlightText(result.Snippet, queryText);
        }

        return results;
    }

    private string ExtractSnippet(string text, string query, int contextLength = 150)
    {
        var index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return text.Length > 300 ? text[..300] + "..." : text;
        }

        var start = Math.Max(0, index - contextLength);
        var end = Math.Min(text.Length, index + query.Length + contextLength);

        var snippet = text[start..end];

        if (start > 0) snippet = "..." + snippet;
        if (end < text.Length) snippet += "...";

        return snippet;
    }

    private string HighlightText(string text, string query)
    {
        // Simple highlighting with ** markers
        return System.Text.RegularExpressions.Regex.Replace(
            text,
            System.Text.RegularExpressions.Regex.Escape(query),
            m => $"**{m.Value}**",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private string EscapeQuery(string query)
    {
        // Escape special Lucene characters
        var special = new[] { '+', '-', '&', '|', '!', '(', ')', '{', '}', '[', ']', '^', '"', '~', '*', '?', ':', '\\', '/' };
        foreach (var c in special)
        {
            query = query.Replace(c.ToString(), "\\" + c);
        }
        return query;
    }

    /// <summary>
    /// מחיקת האינדקס
    /// </summary>
    public void ClearIndex()
    {
        EnsureWriter();
        _writer!.DeleteAll();
        _writer.Commit();
    }

    private void EnsureWriter()
    {
        if (_writer == null)
        {
            _directory = FSDirectory.Open(_indexPath);
            var config = new IndexWriterConfig(AppLuceneVersion, _analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND
            };
            _writer = new IndexWriter(_directory, config);
        }
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _directory?.Dispose();
        _analyzer?.Dispose();
    }
}

/// <summary>
/// תוצאת חיפוש
/// </summary>
public class SearchResult
{
    public int ContentId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string ChapterTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public string HighlightedSnippet { get; set; } = string.Empty;
    public float Score { get; set; }

    public string FullReference => $"{BookTitle} > {ChapterTitle}";
}
