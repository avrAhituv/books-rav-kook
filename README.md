# ספריית הקודש (HolyBooks)

אפליקציית דסקטופ להצגת, חיפוש ולימוד ספרי קודש.

![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)
![Avalonia](https://img.shields.io/badge/Avalonia-11.x-purple)
![License](https://img.shields.io/badge/License-MIT-green)

## תכונות

- 📚 **כתבי הרב קוק** - ייבוא מקבצי Word
- 📖 **תנ"ך עם פרשנים** - רש"י, אבן עזרא, רמב"ן ועוד (מספריא)
- 📜 **תלמוד בבלי** - עם רש"י ותוספות (צורת הדף)
- 🔍 **חיפוש מתקדם** - Full-text search עם Lucene.NET
- 📑 **סימניות והערות** - שמירת מיקומים וכתיבת הערות
- 📄 **דפי מקורות** - יצירת דפים לשיעורים
- 📤 **ייצוא** - Word, PDF

## התקנה

### דרישות
- .NET 8.0 SDK
- Windows / Linux / macOS

### הרצה
```bash
# Clone
git clone https://github.com/your-repo/holybooks.git
cd holybooks

# Restore & Build
dotnet restore
dotnet build

# Run
dotnet run --project src/HolyBooks.Desktop
```

## מבנה הפרויקט

```
HolyBooks/
├── src/
│   ├── HolyBooks.Core/        # Models, Interfaces
│   ├── HolyBooks.Data/        # Database, Search (Lucene)
│   ├── HolyBooks.Desktop/     # Avalonia UI
│   └── HolyBooks.Import/      # Word Import
├── tests/
│   └── HolyBooks.Core.Tests/
└── SPECIFICATION.md           # מסמך איפיון מלא
```

## ייבוא ספרים

### מקבצי Word
1. לחץ על **ייבוא** בתפריט
2. בחר קובץ `.docx`
3. אשר את המבנה שזוהה
4. לחץ **ייבא**

האפליקציה מזהה אוטומטית:
- כותרות לפי **Styles** (Heading 1/2/3)
- או לפי **דפוסים** (פרק א, סעיף א, וכו')

### מספריא (Sefaria)
תנ"ך ותלמוד נטענים אוטומטית מ-API של ספריא.

## קיצורי מקלדת

| פעולה | קיצור |
|-------|-------|
| חיפוש | `Ctrl+F` |
| טאב חדש | `Ctrl+T` |
| סגור טאב | `Ctrl+W` |
| הגדל פונט | `Ctrl++` |
| הקטן פונט | `Ctrl+-` |

## טכנולוגיות

- **UI**: Avalonia 11 (Cross-platform)
- **MVVM**: CommunityToolkit.Mvvm
- **Database**: SQLite + EF Core
- **Search**: Lucene.NET
- **Word Import**: DocumentFormat.OpenXml

## תרומה

1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push (`git push origin feature/amazing`)
5. Open Pull Request

## רישיון

MIT License - ראה [LICENSE](LICENSE)

---

נבנה עם ❤️ לזכר הרב אברהם יצחק הכהן קוק זצ"ל
