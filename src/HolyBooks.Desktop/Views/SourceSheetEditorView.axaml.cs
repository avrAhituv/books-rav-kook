using Avalonia.Controls;
using Avalonia.Input;

namespace HolyBooks.Desktop.Views;

public partial class SourceSheetEditorView : UserControl
{
    public SourceSheetEditorView()
    {
        InitializeComponent();
    }

    private void SearchResult_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Double-click to add
        if (e.ClickCount == 2 && sender is Border border && border.DataContext is SourceSearchResult result)
        {
            if (DataContext is ViewModels.SourceSheetEditorViewModel vm)
            {
                vm.AddSourceFromSearchCommand.Execute(result);
            }
        }
    }
}

// Dummy class for XAML reference - actual class is in ViewModel
public class SourceSearchResult
{
    public string Reference { get; set; } = "";
    public string HebrewRef { get; set; } = "";
    public string Text { get; set; } = "";
}
