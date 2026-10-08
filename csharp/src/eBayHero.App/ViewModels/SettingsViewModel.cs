using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;

namespace eBayHero.App.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly string _settingsPath;
    private bool _showDeveloperSettings;

    public SettingsViewModel(ILogger<SettingsViewModel> logger, string settingsPath)
    {
        _logger = logger;
        _settingsPath = settingsPath;
        LoadSettings();
        
        SaveCommand = new RelayCommand(SaveSettings);
        BrowseSourceRootCommand = new RelayCommand(BrowseSourceRoot);
        BrowseOperationsRootCommand = new RelayCommand(BrowseOperationsRoot);
        BrowseTesseractCommand = new RelayCommand(BrowseTesseract);
    }

    // User Settings
    private string _sourceRoot = @"F:\Inventory";
    public string SourceRoot
    {
        get => _sourceRoot;
        set { _sourceRoot = value; OnPropertyChanged(); }
    }

    private string _operationsRoot = @"D:\INVENTORY_PHOTO_OPS";
    public string OperationsRoot
    {
        get => _operationsRoot;
        set { _operationsRoot = value; OnPropertyChanged(); }
    }

    private string _tesseractPath = @"E:\Apps\tesseract-ocr\tesseract.exe";
    public string TesseractPath
    {
        get => _tesseractPath;
        set { _tesseractPath = value; OnPropertyChanged(); }
    }

    private string _ocrLanguage = "eng";
    public string OcrLanguage
    {
        get => _ocrLanguage;
        set { _ocrLanguage = value; OnPropertyChanged(); }
    }

    private int _ocrMinimumConfidence = 38;
    public int OcrMinimumConfidence
    {
        get => _ocrMinimumConfidence;
        set { _ocrMinimumConfidence = value; OnPropertyChanged(); }
    }

    private string _openAiApiKey = "";
    public string OpenAiApiKey
    {
        get => _openAiApiKey;
        set { _openAiApiKey = value; OnPropertyChanged(); }
    }

    private string _localAiEndpoint = "";
    public string LocalAiEndpoint
    {
        get => _localAiEndpoint;
        set { _localAiEndpoint = value; OnPropertyChanged(); }
    }

    // Developer Settings (hidden by default)
    private string _devDatabasePath = "";
    public string DevDatabasePath
    {
        get => _devDatabasePath;
        set { _devDatabasePath = value; OnPropertyChanged(); }
    }

    private string _devLogPath = "";
    public string DevLogPath
    {
        get => _devLogPath;
        set { _devLogPath = value; OnPropertyChanged(); }
    }

    private bool _devEnableDebugLogging = false;
    public bool DevEnableDebugLogging
    {
        get => _devEnableDebugLogging;
        set { _devEnableDebugLogging = value; OnPropertyChanged(); }
    }

    public bool ShowDeveloperSettings
    {
        get => _showDeveloperSettings;
        set { _showDeveloperSettings = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }
    public ICommand BrowseSourceRootCommand { get; }
    public ICommand BrowseOperationsRootCommand { get; }
    public ICommand BrowseTesseractCommand { get; }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogInformation("Settings file not found, using defaults");
                return;
            }

            var json = File.ReadAllText(_settingsPath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("User", out var user))
            {
                GetString(user, "SourceRoot", v => SourceRoot = v);
                GetString(user, "OperationsRoot", v => OperationsRoot = v);
                GetString(user, "TesseractPath", v => TesseractPath = v);
                GetString(user, "OcrLanguage", v => OcrLanguage = v);
                GetInt(user, "OcrMinimumConfidence", v => OcrMinimumConfidence = v);
                GetString(user, "OpenAiApiKey", v => OpenAiApiKey = v);
                GetString(user, "LocalAiEndpoint", v => LocalAiEndpoint = v);
            }

            if (root.TryGetProperty("Developer", out var dev))
            {
                GetString(dev, "DatabasePath", v => DevDatabasePath = v);
                GetString(dev, "LogPath", v => DevLogPath = v);
                GetBool(dev, "EnableDebugLogging", v => DevEnableDebugLogging = v);
            }

            _logger.LogInformation("Settings loaded from {Path}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}", _settingsPath);
        }
    }

    private void SaveSettings()
    {
        try
        {
            var settings = new
            {
                User = new
                {
                    SourceRoot,
                    OperationsRoot,
                    TesseractPath,
                    OcrLanguage,
                    OcrMinimumConfidence,
                    OpenAiApiKey,
                    LocalAiEndpoint
                },
                Developer = new
                {
                    DatabasePath = DevDatabasePath,
                    LogPath = DevLogPath,
                    EnableDebugLogging = DevEnableDebugLogging
                }
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(_settingsPath, json);

            _logger.LogInformation("Settings saved to {Path}", _settingsPath);
            MessageBox.Show("Settings saved successfully. Restart the application for changes to take effect.", 
                "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", _settingsPath);
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BrowseSourceRoot()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Source Root Directory",
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Select Folder",
            Filter = "Folders|*.none"
        };
        
        if (dialog.ShowDialog() == true)
        {
            var path = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(path))
            {
                SourceRoot = path;
            }
        }
    }

    private void BrowseOperationsRoot()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Operations Root Directory",
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Select Folder",
            Filter = "Folders|*.none"
        };
        
        if (dialog.ShowDialog() == true)
        {
            var path = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(path))
            {
                OperationsRoot = path;
            }
        }
    }

    private void BrowseTesseract()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Tesseract Executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = TesseractPath
        };
        if (dialog.ShowDialog() == true)
        {
            TesseractPath = dialog.FileName;
        }
    }

    private static void GetString(JsonElement element, string property, Action<string> setter)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                setter(text);
            }
        }
    }

    private static void GetInt(JsonElement element, string property, Action<int> setter)
    {
        if (element.TryGetProperty(property, out var value) && value.TryGetInt32(out var number))
        {
            setter(number);
        }
    }

    private static void GetBool(JsonElement element, string property, Action<bool> setter)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
        {
            setter(value.GetBoolean());
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}