using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HolyBooks.Import;
using HolyBooks.Data.Database;

namespace HolyBooks.Desktop.ViewModels;

public partial class ImportWizardViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private readonly ImportService _importService;
    private readonly Window _window;

    [ObservableProperty]
    private int _currentStep = 1;

    // Step indicators
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;

    public string Step1Background => CurrentStep >= 1 ? "#1976D2" : "#90CAF9";
    public string Step2Background => CurrentStep >= 2 ? "#1976D2" : "#90CAF9";
    public string Step3Background => CurrentStep >= 3 ? "#1976D2" : "#90CAF9";

    // Navigation
    public bool CanGoPrevious => CurrentStep > 1 && !IsImporting;
    public bool CanGoNext => CurrentStep < 3 && HasFiles && !IsImporting;
    public bool CanImport => CurrentStep == 2 && HasFiles && !IsImporting;

    // File selection
    public ObservableCollection<ImportFileItem> SelectedFiles { get; } = new();
    public bool HasFiles => SelectedFiles.Any();

    // Preview
    public ObservableCollection<PreviewNode> PreviewNodes { get; } = new();

    [ObservableProperty]
    private string _selectedPreviewText = string.Empty;

    // Import progress
    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private double _importProgress;

    [ObservableProperty]
    private string _importStatusText = "מתחיל ייבוא...";

    [ObservableProperty]
    private string _currentFileText = string.Empty;

    [ObservableProperty]
    private bool _importComplete;

    [ObservableProperty]
    private int _importedBooksCount;

    [ObservableProperty]
    private int _importedChaptersCount;

    public ImportWizardViewModel(AppDbContext db, Window window)
    {
        _db = db;
        _window = window;
        _importService = new ImportService(db);

        SelectedFiles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFiles));
    }

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(Step1Background));
        OnPropertyChanged(nameof(Step2Background));
        OnPropertyChanged(nameof(Step3Background));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanImport));
    }

    [RelayCommand]
    private async Task SelectFiles()
    {
        var storageProvider = _window.StorageProvider;

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "בחר קבצי Word",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Word Documents")
                {
                    Patterns = new[] { "*.docx", "*.doc" }
                }
            }
        });

        foreach (var file in files)
        {
            var path = file.Path.LocalPath;
            if (!SelectedFiles.Any(f => f.FilePath == path))
            {
                var item = new ImportFileItem
                {
                    FilePath = path,
                    FileName = file.Name,
                    Status = FileAnalysisStatus.Pending
                };

                SelectedFiles.Add(item);

                // Analyze file in background
                _ = AnalyzeFileAsync(item);
            }
        }
    }

    private async Task AnalyzeFileAsync(ImportFileItem item)
    {
        item.Status = FileAnalysisStatus.Analyzing;

        await Task.Run(() =>
        {
            var analysis = _importService.AnalyzeFile(item.FilePath);

            item.DetectedChapters = analysis.DetectedChapters;
            item.DetectedSections = analysis.DetectedSections;
            item.HasStyles = analysis.HasStyles;

            item.Status = analysis.Status switch
            {
                FileStatus.Ready => FileAnalysisStatus.Ready,
                FileStatus.NeedsReview => FileAnalysisStatus.NeedsReview,
                FileStatus.Error => FileAnalysisStatus.Error,
                _ => FileAnalysisStatus.Pending
            };

            item.ErrorMessage = analysis.ErrorMessage;
        });
    }

    [RelayCommand]
    private void RemoveFile(ImportFileItem file)
    {
        SelectedFiles.Remove(file);
    }

    [RelayCommand]
    private void Next()
    {
        if (CurrentStep == 1 && HasFiles)
        {
            // Generate preview
            GeneratePreview();
            CurrentStep = 2;
        }
    }

    [RelayCommand]
    private void Previous()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
        }
    }

    private void GeneratePreview()
    {
        PreviewNodes.Clear();

        foreach (var file in SelectedFiles.Where(f => f.Status == FileAnalysisStatus.Ready || f.Status == FileAnalysisStatus.NeedsReview))
        {
            var result = _importService.PreviewImport(file.FilePath);

            if (result.Success && result.Book != null)
            {
                var bookNode = new PreviewNode
                {
                    Title = result.Book.Title,
                    NodeType = "Book",
                    SourceFile = file.FilePath
                };

                foreach (var chapter in result.Book.Chapters)
                {
                    var chapterNode = new PreviewNode
                    {
                        Title = chapter.Title,
                        NodeType = "Chapter",
                        ContentPreview = chapter.Content?.TextContent?[..Math.Min(200, chapter.Content.TextContent.Length)] ?? ""
                    };

                    foreach (var section in chapter.Children)
                    {
                        chapterNode.Children.Add(new PreviewNode
                        {
                            Title = section.Title,
                            NodeType = "Section",
                            ContentPreview = section.Content?.TextContent?[..Math.Min(200, section.Content.TextContent.Length)] ?? ""
                        });
                    }

                    bookNode.Children.Add(chapterNode);
                }

                PreviewNodes.Add(bookNode);
            }
        }
    }

    [RelayCommand]
    private async Task Import()
    {
        CurrentStep = 3;
        IsImporting = true;
        ImportProgress = 0;
        ImportStatusText = "מייבא ספרים...";

        var totalFiles = SelectedFiles.Count;
        var processedFiles = 0;
        var totalChapters = 0;

        foreach (var file in SelectedFiles)
        {
            CurrentFileText = file.FileName;

            var result = await _importService.ImportFileAsync(file.FilePath);

            if (result.Success)
            {
                totalChapters += result.Stats?.TotalChapters ?? 0;
            }

            processedFiles++;
            ImportProgress = (processedFiles * 100.0) / totalFiles;
        }

        ImportedBooksCount = processedFiles;
        ImportedChaptersCount = totalChapters;
        ImportComplete = true;
        IsImporting = false;
        ImportStatusText = "הייבוא הושלם!";
    }

    [RelayCommand]
    private void Cancel()
    {
        _window.Close(false);
    }

    [RelayCommand]
    private void Finish()
    {
        _window.Close(true);
    }
}

public partial class ImportFileItem : ObservableObject
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    [ObservableProperty]
    private FileAnalysisStatus _status;

    public int DetectedChapters { get; set; }
    public int DetectedSections { get; set; }
    public bool HasStyles { get; set; }
    public string? ErrorMessage { get; set; }

    public string StatusIcon => Status switch
    {
        FileAnalysisStatus.Pending => "⏳",
        FileAnalysisStatus.Analyzing => "🔄",
        FileAnalysisStatus.Ready => "✅",
        FileAnalysisStatus.NeedsReview => "⚠️",
        FileAnalysisStatus.Error => "❌",
        _ => "?"
    };

    public string StatusText => Status switch
    {
        FileAnalysisStatus.Pending => "ממתין",
        FileAnalysisStatus.Analyzing => "מנתח...",
        FileAnalysisStatus.Ready => $"{DetectedChapters} פרקים",
        FileAnalysisStatus.NeedsReview => "דורש בדיקה",
        FileAnalysisStatus.Error => ErrorMessage ?? "שגיאה",
        _ => ""
    };
}

public enum FileAnalysisStatus
{
    Pending,
    Analyzing,
    Ready,
    NeedsReview,
    Error
}

public partial class PreviewNode : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _nodeType = "Chapter";

    public string SourceFile { get; set; } = string.Empty;
    public string ContentPreview { get; set; } = string.Empty;

    public ObservableCollection<PreviewNode> Children { get; } = new();
}
