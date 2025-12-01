using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HolyBooks.Data.Services;

namespace HolyBooks.Desktop.ViewModels;

/// <summary>
/// ViewModel לתצוגת תנ"ך עם פרשנים
/// </summary>
public partial class TanakhViewModel : ObservableObject
{
    private readonly SefariaService _sefaria;

    // Current location
    [ObservableProperty]
    private string _selectedBook = "Genesis";

    [ObservableProperty]
    private int _selectedChapter = 1;

    [ObservableProperty]
    private string _hebrewBookName = "בראשית";

    [ObservableProperty]
    private bool _isLoading;

    // Verses
    public ObservableCollection<VerseViewModel> Verses { get; } = new();

    // Selected commentaries
    public ObservableCollection<string> AvailableCommentaries { get; } = new()
    {
        "רש\"י", "אבן עזרא", "רמב\"ן", "ספורנו", "אור החיים", "מצודת דוד", "מצודת ציון", "מלבי\"ם"
    };

    public ObservableCollection<string> SelectedCommentaries { get; } = new()
    {
        "רש\"י", "אבן עזרא"
    };

    // Book structure
    public ObservableCollection<TanakhBookInfo> Books { get; } = new();

    // Number of chapters in current book
    [ObservableProperty]
    private int _totalChapters = 50;

    public TanakhViewModel(SefariaService sefaria)
    {
        _sefaria = sefaria;
        InitializeBooks();
    }

    private void InitializeBooks()
    {
        // Torah
        Books.Add(new TanakhBookInfo { EnglishName = "Genesis", HebrewName = "בראשית", Chapters = 50, Category = "תורה" });
        Books.Add(new TanakhBookInfo { EnglishName = "Exodus", HebrewName = "שמות", Chapters = 40, Category = "תורה" });
        Books.Add(new TanakhBookInfo { EnglishName = "Leviticus", HebrewName = "ויקרא", Chapters = 27, Category = "תורה" });
        Books.Add(new TanakhBookInfo { EnglishName = "Numbers", HebrewName = "במדבר", Chapters = 36, Category = "תורה" });
        Books.Add(new TanakhBookInfo { EnglishName = "Deuteronomy", HebrewName = "דברים", Chapters = 34, Category = "תורה" });

        // Neviim Rishonim
        Books.Add(new TanakhBookInfo { EnglishName = "Joshua", HebrewName = "יהושע", Chapters = 24, Category = "נביאים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Judges", HebrewName = "שופטים", Chapters = 21, Category = "נביאים" });
        Books.Add(new TanakhBookInfo { EnglishName = "I Samuel", HebrewName = "שמואל א", Chapters = 31, Category = "נביאים" });
        Books.Add(new TanakhBookInfo { EnglishName = "II Samuel", HebrewName = "שמואל ב", Chapters = 24, Category = "נביאים" });
        Books.Add(new TanakhBookInfo { EnglishName = "I Kings", HebrewName = "מלכים א", Chapters = 22, Category = "נביאים" });
        Books.Add(new TanakhBookInfo { EnglishName = "II Kings", HebrewName = "מלכים ב", Chapters = 25, Category = "נביאים" });

        // Neviim Acharonim
        Books.Add(new TanakhBookInfo { EnglishName = "Isaiah", HebrewName = "ישעיהו", Chapters = 66, Category = "נביאים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Jeremiah", HebrewName = "ירמיהו", Chapters = 52, Category = "נביאים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Ezekiel", HebrewName = "יחזקאל", Chapters = 48, Category = "נביאים" });

        // Trei Asar
        Books.Add(new TanakhBookInfo { EnglishName = "Hosea", HebrewName = "הושע", Chapters = 14, Category = "תרי עשר" });
        Books.Add(new TanakhBookInfo { EnglishName = "Joel", HebrewName = "יואל", Chapters = 4, Category = "תרי עשר" });
        Books.Add(new TanakhBookInfo { EnglishName = "Amos", HebrewName = "עמוס", Chapters = 9, Category = "תרי עשר" });
        Books.Add(new TanakhBookInfo { EnglishName = "Obadiah", HebrewName = "עובדיה", Chapters = 1, Category = "תרי עשר" });
        Books.Add(new TanakhBookInfo { EnglishName = "Jonah", HebrewName = "יונה", Chapters = 4, Category = "תרי עשר" });
        Books.Add(new TanakhBookInfo { EnglishName = "Micah", HebrewName = "מיכה", Chapters = 7, Category = "תרי עשר" });
        Books.Add(new TanakhBookInfo { EnglishName = "Nahum", HebrewName = "נחום", Chapters = 3, Category = "תרי עשר" });
        Books.Add(new TanakhBookInfo { EnglishName = "Habakkuk", HebrewName = "חבקוק", Chapters = 3, Category = "תרי עשר" });
        Books.Add(new TanakhBookInfo { EnglishName = "Zephaniah", HebrewName = "צפניה", Chapters = 3, Category = "תרי עשר" });
        Books.Add(new TanakhBookInfo { EnglishName = "Haggai", HebrewName = "חגי", Chapters = 2, Category = "תרי עשר" });
        Books.Add(new TanakhBookInfo { EnglishName = "Zechariah", HebrewName = "זכריה", Chapters = 14, Category = "תרי עשר" });
        Books.Add(new TanakhBookInfo { EnglishName = "Malachi", HebrewName = "מלאכי", Chapters = 3, Category = "תרי עשר" });

        // Ketuvim
        Books.Add(new TanakhBookInfo { EnglishName = "Psalms", HebrewName = "תהלים", Chapters = 150, Category = "כתובים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Proverbs", HebrewName = "משלי", Chapters = 31, Category = "כתובים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Job", HebrewName = "איוב", Chapters = 42, Category = "כתובים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Song of Songs", HebrewName = "שיר השירים", Chapters = 8, Category = "כתובים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Ruth", HebrewName = "רות", Chapters = 4, Category = "כתובים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Lamentations", HebrewName = "איכה", Chapters = 5, Category = "כתובים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Ecclesiastes", HebrewName = "קהלת", Chapters = 12, Category = "כתובים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Esther", HebrewName = "אסתר", Chapters = 10, Category = "כתובים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Daniel", HebrewName = "דניאל", Chapters = 12, Category = "כתובים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Ezra", HebrewName = "עזרא", Chapters = 10, Category = "כתובים" });
        Books.Add(new TanakhBookInfo { EnglishName = "Nehemiah", HebrewName = "נחמיה", Chapters = 13, Category = "כתובים" });
        Books.Add(new TanakhBookInfo { EnglishName = "I Chronicles", HebrewName = "דברי הימים א", Chapters = 29, Category = "כתובים" });
        Books.Add(new TanakhBookInfo { EnglishName = "II Chronicles", HebrewName = "דברי הימים ב", Chapters = 36, Category = "כתובים" });
    }

    [RelayCommand]
    public async Task LoadChapter()
    {
        IsLoading = true;
        Verses.Clear();

        try
        {
            var bookInfo = Books.FirstOrDefault(b => b.EnglishName == SelectedBook);
            if (bookInfo != null)
            {
                HebrewBookName = bookInfo.HebrewName;
                TotalChapters = bookInfo.Chapters;
            }

            var chapter = await _sefaria.GetTanakhChapterAsync(SelectedBook, SelectedChapter);

            if (chapter != null)
            {
                for (int i = 0; i < chapter.HebrewText.Count; i++)
                {
                    var verse = new VerseViewModel
                    {
                        VerseNumber = i + 1,
                        HebrewText = chapter.HebrewText[i],
                        EnglishText = i < chapter.EnglishText.Count ? chapter.EnglishText[i] : ""
                    };

                    // Add commentaries
                    foreach (var commentaryName in SelectedCommentaries)
                    {
                        if (chapter.Commentaries.TryGetValue(commentaryName, out var commentaries))
                        {
                            if (i < commentaries.Count && commentaries[i].Any())
                            {
                                verse.Commentaries.Add(new CommentaryViewModel
                                {
                                    Name = commentaryName,
                                    Text = string.Join("\n", commentaries[i])
                                });
                            }
                        }
                    }

                    Verses.Add(verse);
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectBook(TanakhBookInfo book)
    {
        SelectedBook = book.EnglishName;
        HebrewBookName = book.HebrewName;
        TotalChapters = book.Chapters;
        SelectedChapter = 1;
        await LoadChapter();
    }

    [RelayCommand]
    public async Task NextChapter()
    {
        if (SelectedChapter < TotalChapters)
        {
            SelectedChapter++;
            await LoadChapter();
        }
    }

    [RelayCommand]
    public async Task PreviousChapter()
    {
        if (SelectedChapter > 1)
        {
            SelectedChapter--;
            await LoadChapter();
        }
    }

    [RelayCommand]
    public void ToggleCommentary(string commentary)
    {
        if (SelectedCommentaries.Contains(commentary))
        {
            SelectedCommentaries.Remove(commentary);
        }
        else
        {
            SelectedCommentaries.Add(commentary);
        }
    }
}

public class TanakhBookInfo
{
    public string EnglishName { get; set; } = "";
    public string HebrewName { get; set; } = "";
    public int Chapters { get; set; }
    public string Category { get; set; } = "";
}

public partial class VerseViewModel : ObservableObject
{
    [ObservableProperty]
    private int _verseNumber;

    [ObservableProperty]
    private string _hebrewText = "";

    [ObservableProperty]
    private string _englishText = "";

    [ObservableProperty]
    private bool _isExpanded;

    public ObservableCollection<CommentaryViewModel> Commentaries { get; } = new();

    public string HebrewVerseNumber => ToHebrewLetter(VerseNumber);

    private string ToHebrewLetter(int num)
    {
        var ones = new[] { "", "א", "ב", "ג", "ד", "ה", "ו", "ז", "ח", "ט" };
        var tens = new[] { "", "י", "כ", "ל", "מ", "נ", "ס", "ע", "פ", "צ" };

        if (num < 10) return ones[num];
        if (num == 15) return "טו";
        if (num == 16) return "טז";
        if (num < 100)
        {
            return tens[num / 10] + ones[num % 10];
        }
        return num.ToString();
    }
}

public class CommentaryViewModel
{
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";
}
