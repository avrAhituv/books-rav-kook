using Avalonia.Controls;

namespace HolyBooks.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void NavigationList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Navigation is handled via binding to SelectedNavigationIndex
        // This handler is for any additional logic needed on navigation change
    }
}
