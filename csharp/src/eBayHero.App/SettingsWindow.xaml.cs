using System.Windows;
using eBayHero.App.ViewModels;

namespace eBayHero.App;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        
        // Load the OpenAI API key into the PasswordBox
        if (viewModel.OpenAiApiKey != null)
        {
            OpenAiKeyBox.Password = viewModel.OpenAiApiKey;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OpenAiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenAiApiKey = OpenAiKeyBox.Password;
    }
}