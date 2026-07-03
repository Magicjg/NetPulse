using System.Windows;
using System.Windows.Media;

namespace NetPulse.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        EnsureMutableThemeResources();
        base.OnStartup(e);
    }

    private void EnsureMutableThemeResources()
    {
        Resources["AppBackgroundBrush"] = CreateSolidBrush("#07111F");
        Resources["PanelBrush"] = CreateSolidBrush("#101B2E");
        Resources["PanelSoftBrush"] = CreateSolidBrush("#18243A");
        Resources["PanelBorderBrush"] = CreateSolidBrush("#24334D");
        Resources["AccentBrush"] = CreateSolidBrush("#2DD4BF");
        Resources["AccentAltBrush"] = CreateSolidBrush("#4F8CFF");
        Resources["AccentPinkBrush"] = CreateSolidBrush("#FB7185");
        Resources["AccentSoftBrush"] = CreateSolidBrush("#123B46");
        Resources["SummaryPanelBrush"] = CreateSolidBrush("#111D31");
        Resources["SourcePanelBrush"] = CreateSolidBrush("#0F1828");
        Resources["ChipNeutralBrush"] = CreateSolidBrush("#27314B");
        Resources["ChipAccentBrush"] = CreateSolidBrush("#193842");
        Resources["ChipAltBrush"] = CreateSolidBrush("#243764");
        Resources["TextBrush"] = CreateSolidBrush("#F8FAFC");
        Resources["MutedBrush"] = CreateSolidBrush("#9FB3C8");
        Resources["WarningBrush"] = CreateSolidBrush("#FBBF24");
        Resources["HeroGradientBrush"] = CreateHeroGradient("#13233D", "#0A1527", "#0B2330");
    }

    private static SolidColorBrush CreateSolidBrush(string hexColor)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
    }

    private static LinearGradientBrush CreateHeroGradient(string first, string second, string third)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };

        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(first), 0));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(second), 0.55));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(third), 1));
        return brush;
    }
}
