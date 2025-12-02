using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace HolyBooks.Desktop.Converters;

/// <summary>
/// ממיר לבדיקת שוויון ערכים
/// </summary>
public class EqualConverter : IValueConverter
{
    public static readonly EqualConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return parameter;
        return null;
    }
}

/// <summary>
/// ממיר לבדיקת אי-שוויון ערכים
/// </summary>
public class NotEqualConverter : IValueConverter
{
    public static readonly NotEqualConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() != parameter?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// ממיר בוליאני לטקסט "שונה/נשמר"
/// </summary>
public class DirtyTextConverter : IValueConverter
{
    public static readonly DirtyTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "שינויים לא נשמרו" : "נשמר";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// ממיר לבדיקה אם רשימה מכילה ערך
/// Value = collection, Parameter = item to check
/// </summary>
public class ContainsConverter : IValueConverter
{
    public static readonly ContainsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable<string> collection && parameter is string item)
        {
            return collection.Contains(item);
        }

        if (value is System.Collections.IEnumerable enumerable && parameter != null)
        {
            foreach (var elem in enumerable)
            {
                if (elem?.Equals(parameter) == true || elem?.ToString() == parameter.ToString())
                    return true;
            }
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// ממיר לניקוי טקסט HTML
/// </summary>
public class HtmlToPlainTextConverter : IValueConverter
{
    public static readonly HtmlToPlainTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string html)
            return string.Empty;

        // Remove HTML tags
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");

        // Decode HTML entities
        text = System.Net.WebUtility.HtmlDecode(text);

        return text.Trim();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// ממיר מספר לעברית (גימטריא)
/// </summary>
public class NumberToHebrewConverter : IValueConverter
{
    public static readonly NumberToHebrewConverter Instance = new();

    private static readonly string[] Ones = { "", "א", "ב", "ג", "ד", "ה", "ו", "ז", "ח", "ט" };
    private static readonly string[] Tens = { "", "י", "כ", "ל", "מ", "נ", "ס", "ע", "פ", "צ" };
    private static readonly string[] Hundreds = { "", "ק", "ר", "ש", "ת", "תק", "תר", "תש", "תת", "תתק" };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int num || num < 1 || num > 999)
            return value?.ToString() ?? "";

        var result = "";

        if (num >= 100)
        {
            result += Hundreds[num / 100];
            num %= 100;
        }

        if (num >= 10)
        {
            // Special cases for 15 and 16
            if (num == 15)
                return result + "טו";
            if (num == 16)
                return result + "טז";

            result += Tens[num / 10];
            num %= 10;
        }

        result += Ones[num];

        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// ממיר בוליאני הפוך
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public static readonly InverseBooleanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : value;
    }
}

/// <summary>
/// ממיר בוליאני לשקיפות
/// </summary>
public class BooleanToOpacityConverter : IValueConverter
{
    public static readonly BooleanToOpacityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var opacity = parameter is string s && double.TryParse(s, out var d) ? d : 0.5;
        return value is true ? 1.0 : opacity;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// ממיר ערכת נושא לצבעים
/// </summary>
public class ThemeToColorConverter : IValueConverter
{
    public static readonly ThemeToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var theme = value?.ToString() ?? "light";
        var colorType = parameter?.ToString() ?? "background";

        return (theme, colorType) switch
        {
            ("light", "background") => Brush.Parse("#FFFFFF"),
            ("light", "text") => Brush.Parse("#212121"),
            ("light", "secondary") => Brush.Parse("#F5F5F5"),

            ("dark", "background") => Brush.Parse("#1E1E1E"),
            ("dark", "text") => Brush.Parse("#E0E0E0"),
            ("dark", "secondary") => Brush.Parse("#2D2D2D"),

            ("sepia", "background") => Brush.Parse("#F5F1E6"),
            ("sepia", "text") => Brush.Parse("#5D4037"),
            ("sepia", "secondary") => Brush.Parse("#EDE7D8"),

            _ => Brush.Parse("#FFFFFF")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// ממיר קיצור טקסט
/// </summary>
public class TruncateConverter : IValueConverter
{
    public static readonly TruncateConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text)
            return string.Empty;

        var maxLength = parameter is string s && int.TryParse(s, out var len) ? len : 100;

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// ממיר תאריך עברי
/// </summary>
public class HebrewDateConverter : IValueConverter
{
    public static readonly HebrewDateConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime date)
            return string.Empty;

        var hebrewCalendar = new HebrewCalendar();
        var hebrewCulture = new CultureInfo("he-IL");
        hebrewCulture.DateTimeFormat.Calendar = hebrewCalendar;

        try
        {
            return date.ToString("d MMMM yyyy", hebrewCulture);
        }
        catch
        {
            return date.ToString("dd/MM/yyyy");
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// ממיר זמן יחסי (לפני X דקות, לפני Y ימים)
/// </summary>
public class RelativeTimeConverter : IValueConverter
{
    public static readonly RelativeTimeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime date)
            return string.Empty;

        var diff = DateTime.Now - date;

        return diff.TotalMinutes switch
        {
            < 1 => "עכשיו",
            < 60 => $"לפני {(int)diff.TotalMinutes} דקות",
            < 1440 => $"לפני {(int)diff.TotalHours} שעות",
            < 10080 => $"לפני {(int)diff.TotalDays} ימים",
            < 43200 => $"לפני {(int)(diff.TotalDays / 7)} שבועות",
            _ => date.ToString("dd/MM/yyyy")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
