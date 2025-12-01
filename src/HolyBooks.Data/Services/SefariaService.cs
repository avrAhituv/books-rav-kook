using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HolyBooks.Data.Database;
using HolyBooks.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace HolyBooks.Data.Services;

/// <summary>
/// שירות לגישה ל-API של ספריא
/// </summary>
public class SefariaService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _db;
    private const string BaseUrl = "https://www.sefaria.org/api";

    public SefariaService(AppDbContext db)
    {
        _db = db;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "HolyBooks-Desktop/1.0");
    }

    /// <summary>
    /// קבלת טקסט לפי הפניה
    /// </summary>
    public async Task<SefariaText?> GetTextAsync(string reference, bool withCommentary = false)
    {
        // Check cache first
        var cached = await GetFromCacheAsync(reference);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<SefariaText>(cached);
        }

        try
        {
            var url = $"/texts/{Uri.EscapeDataString(reference)}";
            if (withCommentary)
            {
                url += "?commentary=1";
            }

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();

            // Cache the result
            await SaveToCacheAsync(reference, json, withCommentary ? "text_with_commentary" : "text");

            return JsonSerializer.Deserialize<SefariaText>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// קבלת פרק תנ"ך עם פרשנים
    /// </summary>
    public async Task<TanakhChapter?> GetTanakhChapterAsync(string book, int chapter)
    {
        var reference = $"{book}.{chapter}";
        var text = await GetTextAsync(reference, withCommentary: true);

        if (text == null) return null;

        var result = new TanakhChapter
        {
            Book = book,
            Chapter = chapter,
            HebrewText = text.He ?? new List<string>(),
            EnglishText = text.Text ?? new List<string>()
        };

        // Get commentaries
        if (text.Commentary != null)
        {
            foreach (var commentary in text.Commentary)
            {
                var commentaryName = GetCommentaryDisplayName(commentary.CollectiveTitle ?? "");
                if (!result.Commentaries.ContainsKey(commentaryName))
                {
                    result.Commentaries[commentaryName] = new List<List<string>>();
                }

                // Group by verse
                if (commentary.He != null)
                {
                    result.Commentaries[commentaryName].Add(commentary.He);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// קבלת דף גמרא
    /// </summary>
    public async Task<TalmudPage?> GetTalmudPageAsync(string tractate, string daf)
    {
        var reference = $"{tractate}.{daf}";
        var text = await GetTextAsync(reference, withCommentary: true);

        if (text == null) return null;

        var result = new TalmudPage
        {
            Tractate = tractate,
            Daf = daf,
            GemaraText = text.He ?? new List<string>()
        };

        // Get Rashi and Tosafot
        if (text.Commentary != null)
        {
            foreach (var commentary in text.Commentary)
            {
                var title = commentary.CollectiveTitle?.ToLower() ?? "";

                if (title.Contains("rashi") || title.Contains("רש\"י"))
                {
                    result.RashiText = commentary.He ?? new List<string>();
                }
                else if (title.Contains("tosafot") || title.Contains("תוספות"))
                {
                    result.TosafotText = commentary.He ?? new List<string>();
                }
            }
        }

        return result;
    }

    /// <summary>
    /// קבלת רשימת ספרים
    /// </summary>
    public async Task<List<SefariaIndexEntry>?> GetIndexAsync()
    {
        var cached = await GetFromCacheAsync("_index");
        if (cached != null)
        {
            return JsonSerializer.Deserialize<List<SefariaIndexEntry>>(cached);
        }

        try
        {
            var response = await _httpClient.GetAsync("/index");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            await SaveToCacheAsync("_index", json, "index", TimeSpan.FromDays(7));

            return JsonSerializer.Deserialize<List<SefariaIndexEntry>>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// חיפוש בספריא
    /// </summary>
    public async Task<SefariaSearchResult?> SearchAsync(string query, string? filters = null, int limit = 50)
    {
        try
        {
            var url = $"/search-wrapper?q={Uri.EscapeDataString(query)}&size={limit}";
            if (!string.IsNullOrEmpty(filters))
            {
                url += $"&filters={Uri.EscapeDataString(filters)}";
            }

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<SefariaSearchResult>();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// קבלת קישורים לטקסט
    /// </summary>
    public async Task<List<SefariaLink>?> GetLinksAsync(string reference)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/links/{Uri.EscapeDataString(reference)}");
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<List<SefariaLink>>();
        }
        catch
        {
            return null;
        }
    }

    #region Cache

    private async Task<string?> GetFromCacheAsync(string reference)
    {
        var cache = await _db.SefariaCache
            .FirstOrDefaultAsync(c => c.Ref == reference);

        if (cache == null) return null;

        // Check expiration
        if (cache.ExpiresAt.HasValue && cache.ExpiresAt < DateTime.UtcNow)
        {
            _db.SefariaCache.Remove(cache);
            await _db.SaveChangesAsync();
            return null;
        }

        return cache.ContentJson;
    }

    private async Task SaveToCacheAsync(string reference, string json, string contentType, TimeSpan? expiration = null)
    {
        var existing = await _db.SefariaCache
            .FirstOrDefaultAsync(c => c.Ref == reference);

        if (existing != null)
        {
            existing.ContentJson = json;
            existing.FetchedAt = DateTime.UtcNow;
            existing.ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null;
        }
        else
        {
            _db.SefariaCache.Add(new SefariaCache
            {
                Ref = reference,
                ContentJson = json,
                ContentType = contentType,
                FetchedAt = DateTime.UtcNow,
                ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null
            });
        }

        await _db.SaveChangesAsync();
    }

    #endregion

    private string GetCommentaryDisplayName(string title) => title switch
    {
        "Rashi" => "רש\"י",
        "Ibn Ezra" => "אבן עזרא",
        "Ramban" => "רמב\"ן",
        "Sforno" => "ספורנו",
        "Or HaChaim" => "אור החיים",
        "Metzudat David" => "מצודת דוד",
        "Metzudat Zion" => "מצודת ציון",
        "Tosafot" => "תוספות",
        _ => title
    };

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

#region DTOs

public class SefariaText
{
    [JsonPropertyName("he")]
    public List<string>? He { get; set; }

    [JsonPropertyName("text")]
    public List<string>? Text { get; set; }

    [JsonPropertyName("ref")]
    public string? Ref { get; set; }

    [JsonPropertyName("heRef")]
    public string? HeRef { get; set; }

    [JsonPropertyName("sectionRef")]
    public string? SectionRef { get; set; }

    [JsonPropertyName("commentary")]
    public List<SefariaCommentary>? Commentary { get; set; }
}

public class SefariaCommentary
{
    [JsonPropertyName("he")]
    public List<string>? He { get; set; }

    [JsonPropertyName("text")]
    public List<string>? Text { get; set; }

    [JsonPropertyName("collectiveTitle")]
    public string? CollectiveTitle { get; set; }

    [JsonPropertyName("ref")]
    public string? Ref { get; set; }
}

public class SefariaIndexEntry
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("heTitle")]
    public string? HeTitle { get; set; }

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; set; }
}

public class SefariaSearchResult
{
    [JsonPropertyName("hits")]
    public SefariaSearchHits? Hits { get; set; }
}

public class SefariaSearchHits
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("hits")]
    public List<SefariaSearchHit>? Hits { get; set; }
}

public class SefariaSearchHit
{
    [JsonPropertyName("_source")]
    public SefariaSearchSource? Source { get; set; }
}

public class SefariaSearchSource
{
    [JsonPropertyName("ref")]
    public string? Ref { get; set; }

    [JsonPropertyName("heRef")]
    public string? HeRef { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

public class SefariaLink
{
    [JsonPropertyName("ref")]
    public string? Ref { get; set; }

    [JsonPropertyName("heRef")]
    public string? HeRef { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

#endregion

#region Result Models

public class TanakhChapter
{
    public string Book { get; set; } = string.Empty;
    public int Chapter { get; set; }
    public List<string> HebrewText { get; set; } = new();
    public List<string> EnglishText { get; set; } = new();
    public Dictionary<string, List<List<string>>> Commentaries { get; set; } = new();
}

public class TalmudPage
{
    public string Tractate { get; set; } = string.Empty;
    public string Daf { get; set; } = string.Empty;
    public List<string> GemaraText { get; set; } = new();
    public List<string> RashiText { get; set; } = new();
    public List<string> TosafotText { get; set; } = new();
}

#endregion
