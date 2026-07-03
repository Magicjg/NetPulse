using System.Windows;
using NetPulse.App.Services;
using NetPulse.App.ViewModels;

namespace NetPulse.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var settingsService = new AppSettingsService();
        var themeService = new ThemeService(Application.Current, settingsService);
        DataContext = new MainViewModel(new OoklaCliSpeedTestService(), new HistoryPersistenceService(), themeService);
    }
}
