using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HolyBooks.Desktop.Views;
using HolyBooks.Desktop.ViewModels;
using HolyBooks.Desktop.Services;
using HolyBooks.Data.Database;
using HolyBooks.Data.Search;
using HolyBooks.Data.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HolyBooks.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Setup DI
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Initialize database
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Database
        services.AddDbContext<AppDbContext>();

        // Services
        services.AddSingleton<SefariaService>();
        services.AddSingleton<LuceneSearchService>();
        services.AddSingleton<ExportService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<BookTreeViewModel>();
        services.AddTransient<ReaderViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<TanakhViewModel>();
        services.AddTransient<TalmudViewModel>();
        services.AddTransient<SourceSheetEditorViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}
