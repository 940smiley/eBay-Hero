using System.Windows;
using System.IO;
using System.Text.Json;
using eBayHero.App.Logging;
using eBayHero.App.ViewModels;
using eBayHero.Core.Configuration;
using eBayHero.Core.Licensing;
using eBayHero.Core.Services;
using eBayHero.Export;
using eBayHero.FileSystem;
using eBayHero.Infrastructure;
using eBayHero.Infrastructure.Migration;
using eBayHero.Ocr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eBayHero.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            _host?.Services.GetService<ILogger<App>>()?.LogError(args.Exception, "Unhandled UI exception");
            MessageBox.Show(args.Exception.Message, "eBay Hero", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        var allowLive = e.Args.Any(arg => arg.Equals("--allow-live", StringComparison.OrdinalIgnoreCase)) ||
                        string.Equals(Environment.GetEnvironmentVariable("IPO_ALLOW_LIVE"), "1", StringComparison.OrdinalIgnoreCase);
        var options = LoadInventoryOptions(allowLive);
        if (!allowLive)
        {
            options.OperationsRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "eBayHero-dev");
        }
        Directory.CreateDirectory(options.OperationsRoot);
        Directory.CreateDirectory(Path.Combine(options.OperationsRoot, "logs"));

        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddDebug();
                builder.AddProvider(new FileLoggerProvider(
                    Path.Combine(options.OperationsRoot, "logs"),
                    new SecretRedactor()));
            })
            .ConfigureServices(services =>
            {
                var edition =
#if DEVELOPER_BUILD
                    ProductEdition.Development;
#else
                    ProductEdition.Public;
#endif
                services.AddSingleton<IProductAccessService>(
                    new ProductAccessService(options.OperationsRoot, edition));
                services.AddInventoryInfrastructure(options);
                services.AddInventoryFileSystem();
                services.AddInventoryOcr();
                services.AddInventoryExport();
                services.AddSingleton<SettingsViewModel>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<SettingsViewModel>>();
                    var settingsPath = Path.Combine(options.OperationsRoot, "settings.json");
                    return new SettingsViewModel(logger, settingsPath);
                });
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();
        await _host.Services.GetRequiredService<DatabaseMaintenanceService>().EnsureMigratedAsync(CancellationToken.None);

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Show();
        await window.ViewModel.InitializeCommand.ExecuteAsync(null);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static InventoryOptions LoadInventoryOptions(bool allowLive)
    {
        var options = new InventoryOptions { DevelopmentSafeMode = !allowLive };
        var repoRoot = FindRepoRoot();
        ApplyJsonOptions(options, Path.Combine(repoRoot, "config", "paths.json"));
        ApplyJsonOptions(options, Path.Combine(repoRoot, "config.json"));
        options.DevelopmentSafeMode = !allowLive;
        return options;
    }

    private static string FindRepoRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var current = new DirectoryInfo(start);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "eBayHero.sln")) ||
                    File.Exists(Path.Combine(current.FullName, "config.json")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static void ApplyJsonOptions(InventoryOptions options, string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        SetString(root, "OperationsRoot", value => options.OperationsRoot = value);
        SetString(root, "OpsRoot", value => options.OperationsRoot = value);
        SetString(root, "DefaultSourceRoot", value => options.DefaultSourceRoot = value);
        SetString(root, "SourceRoot", value => options.DefaultSourceRoot = value);
        SetString(root, "InventorySource", value => options.DefaultSourceRoot = value);
        SetString(root, "TesseractPath", value => options.TesseractPath = value);
        SetString(root, "OcrLanguage", value => options.OcrLanguage = value);
        SetString(root, "DefaultListingStatus", value => options.DefaultListingStatus = value);
        SetString(root, "DefaultStatus", value => options.DefaultListingStatus = value);
        SetInt(root, "OcrMinimumConfidence", value => options.OcrMinimumConfidence = value);
        SetInt(root, "OcrUpscaleFactor", value => options.OcrUpscaleFactor = value);
        SetInt(root, "AutoGroupSeconds", value => options.AutoGroupSeconds = value);
        SetInt(root, "MaxAutomaticGroupSize", value => options.MaxAutomaticGroupSize = value);
        SetInt(root, "MaxGroupImages", value => options.MaxAutomaticGroupSize = value);
    }

    private static void SetString(JsonElement root, string key, Action<string> setter)
    {
        if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                setter(text);
            }
        }
    }

    private static void SetInt(JsonElement root, string key, Action<int> setter)
    {
        if (root.TryGetProperty(key, out var value) && value.TryGetInt32(out var number))
        {
            setter(number);
        }
    }
}