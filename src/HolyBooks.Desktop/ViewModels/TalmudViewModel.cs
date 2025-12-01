using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HolyBooks.Data.Services;

namespace HolyBooks.Desktop.ViewModels;

/// <summary>
/// ViewModel לתצוגת דף גמרא
/// </summary>
public partial class TalmudViewModel : ObservableObject
{
    private readonly SefariaService _sefaria;

    [ObservableProperty]
    private string _currentTractate = "Berakhot";

    [ObservableProperty]
    private string _currentDaf = "2a";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _hebrewTitle = "מסכת ברכות דף ב עמוד א";

    // Gemara text
    public ObservableCollection<string> GemaraLines { get; } = new();

    // Rashi text
    public ObservableCollection<string> RashiLines { get; } = new();

    // Tosafot text
    public ObservableCollection<string> TosafotLines { get; } = new();

    // Display mode
    [ObservableProperty]
    private TalmudDisplayMode _displayMode = TalmudDisplayMode.TzuratHaDaf;

    // Navigation
    public ObservableCollection<string> AvailableDafim { get; } = new();

    public TalmudViewModel(SefariaService sefaria)
    {
        _sefaria = sefaria;
        InitializeDafim();
    }

    private void InitializeDafim()
    {
        // Standard daf range (simplified - Berakhot has 64 pages)
        for (int i = 2; i <= 64; i++)
        {
            AvailableDafim.Add($"{i}a");
            AvailableDafim.Add($"{i}b");
        }
    }

    [RelayCommand]
    public async Task LoadPage()
    {
        IsLoading = true;
        GemaraLines.Clear();
        RashiLines.Clear();
        TosafotLines.Clear();

        try
        {
            var page = await _sefaria.GetTalmudPageAsync(CurrentTractate, CurrentDaf);

            if (page != null)
            {
                HebrewTitle = GetHebrewTitle();

                foreach (var line in page.GemaraText)
                {
                    GemaraLines.Add(line);
                }

                foreach (var line in page.RashiText)
                {
                    RashiLines.Add(line);
                }

                foreach (var line in page.TosafotText)
                {
                    TosafotLines.Add(line);
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task NextPage()
    {
        var currentIndex = AvailableDafim.IndexOf(CurrentDaf);
        if (currentIndex < AvailableDafim.Count - 1)
        {
            CurrentDaf = AvailableDafim[currentIndex + 1];
            await LoadPage();
        }
    }

    [RelayCommand]
    public async Task PreviousPage()
    {
        var currentIndex = AvailableDafim.IndexOf(CurrentDaf);
        if (currentIndex > 0)
        {
            CurrentDaf = AvailableDafim[currentIndex - 1];
            await LoadPage();
        }
    }

    [RelayCommand]
    public void SetDisplayMode(TalmudDisplayMode mode)
    {
        DisplayMode = mode;
    }

    private string GetHebrewTitle()
    {
        var tractateHe = GetHebrewTractate(CurrentTractate);
        var dafHe = GetHebrewDaf(CurrentDaf);
        return $"מסכת {tractateHe} דף {dafHe}";
    }

    private string GetHebrewTractate(string tractate) => tractate switch
    {
        "Berakhot" => "ברכות",
        "Shabbat" => "שבת",
        "Eruvin" => "עירובין",
        "Pesachim" => "פסחים",
        "Yoma" => "יומא",
        "Sukkah" => "סוכה",
        "Beitzah" => "ביצה",
        "Rosh Hashanah" => "ראש השנה",
        "Taanit" => "תענית",
        "Megillah" => "מגילה",
        "Moed Katan" => "מועד קטן",
        "Chagigah" => "חגיגה",
        "Yevamot" => "יבמות",
        "Ketubot" => "כתובות",
        "Nedarim" => "נדרים",
        "Nazir" => "נזיר",
        "Sotah" => "סוטה",
        "Gittin" => "גיטין",
        "Kiddushin" => "קידושין",
        "Bava Kamma" => "בבא קמא",
        "Bava Metzia" => "בבא מציעא",
        "Bava Batra" => "בבא בתרא",
        "Sanhedrin" => "סנהדרין",
        "Makkot" => "מכות",
        "Shevuot" => "שבועות",
        "Avodah Zarah" => "עבודה זרה",
        "Horayot" => "הוריות",
        "Zevachim" => "זבחים",
        "Menachot" => "מנחות",
        "Chullin" => "חולין",
        "Bekhorot" => "בכורות",
        "Arakhin" => "ערכין",
        "Temurah" => "תמורה",
        "Keritot" => "כריתות",
        "Meilah" => "מעילה",
        "Niddah" => "נדה",
        _ => tractate
    };

    private string GetHebrewDaf(string daf)
    {
        var number = int.Parse(daf.TrimEnd('a', 'b'));
        var amud = daf.EndsWith('a') ? "עמוד א" : "עמוד ב";
        var hebrewNumber = ToHebrewNumber(number);
        return $"{hebrewNumber} {amud}";
    }

    private string ToHebrewNumber(int number)
    {
        var ones = new[] { "", "א", "ב", "ג", "ד", "ה", "ו", "ז", "ח", "ט" };
        var tens = new[] { "", "י", "כ", "ל", "מ", "נ", "ס", "ע", "פ", "צ" };

        if (number < 10) return ones[number];
        if (number < 100)
        {
            var t = number / 10;
            var o = number % 10;

            // Special cases
            if (number == 15) return "טו";
            if (number == 16) return "טז";

            return tens[t] + ones[o];
        }

        return number.ToString();
    }
}

public enum TalmudDisplayMode
{
    /// <summary>
    /// צורת הדף - כמו בדפוס וילנא
    /// </summary>
    TzuratHaDaf,

    /// <summary>
    /// תצוגה ליניארית - טקסט רץ
    /// </summary>
    Linear,

    /// <summary>
    /// תצוגה מודרנית - גמרא במרכז עם פאנלים
    /// </summary>
    Modern
}
