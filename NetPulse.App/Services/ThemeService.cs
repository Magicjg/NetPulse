using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using NetPulse.App.Models;

namespace NetPulse.App.Services;

public sealed class ThemeService
{
    private readonly Application _application;
    private readonly AppSettingsService _settingsService;
    private static readonly Duration ThemeAnimationDuration = new(TimeSpan.FromMilliseconds(260));
    private string _themeMode;

    public ThemeService(Application application, AppSettingsService settingsService)
    {
        _application = application;
        _settingsService = settingsService;
        _themeMode = _settingsService.Load().ThemeMode;
        ApplyTheme(_themeMode);
    }

    public bool IsDarkTheme => _themeMode == "Dark";

    public string ThemeButtonText => IsDarkTheme ? "Modo claro" : "Modo oscuro";

    public string ThemeDescriptionText => IsDarkTheme ? "Tema oscuro activo" : "Tema claro activo";

    public void SetTheme(bool useDarkTheme)
    {
        string nextMode = useDarkTheme ? "Dark" : "Light";
        if (_themeMode == nextMode)
        {
            return;
        }

        _themeMode = nextMode;
        ApplyTheme(_themeMode);
        _settingsService.Save(new AppSettings
        {
            ThemeMode = _themeMode
        });
    }

    public void ToggleTheme()
    {
        SetTheme(!IsDarkTheme);
    }

    private void ApplyTheme(string themeMode)
    {
        if (themeMode == "Light")
        {
            SetBrushColor("AppBackgroundBrush", "#F4F7FC");
            SetBrushColor("PanelBrush", "#FFFFFF");
            SetBrushColor("PanelSoftBrush", "#EDF3FB");
            SetBrushColor("PanelBorderBrush", "#CCD7E8");
            SetBrushColor("AccentBrush", "#0E9384");
            SetBrushColor("AccentAltBrush", "#315EFB");
            SetBrushColor("AccentPinkBrush", "#D94B68");
            SetBrushColor("AccentSoftBrush", "#D8F2EC");
            SetBrushColor("SummaryPanelBrush", "#EBF3FF");
            SetBrushColor("SourcePanelBrush", "#F9FBFF");
            SetBrushColor("ChipNeutralBrush", "#E7EDF7");
            SetBrushColor("ChipAccentBrush", "#DDF5F0");
            SetBrushColor("ChipAltBrush", "#E1E9FF");
            SetBrushColor("TextBrush", "#122033");
            SetBrushColor("MutedBrush", "#5D6D84");
            SetBrushColor("WarningBrush", "#B7791F");
            SetGradientColors("HeroGradientBrush", "#E4EDFF", "#F9FBFF", "#EAF2FF");
        }
        else
        {
            SetBrushColor("AppBackgroundBrush", "#07111F");
            SetBrushColor("PanelBrush", "#101B2E");
            SetBrushColor("PanelSoftBrush", "#18243A");
            SetBrushColor("PanelBorderBrush", "#24334D");
            SetBrushColor("AccentBrush", "#2DD4BF");
            SetBrushColor("AccentAltBrush", "#4F8CFF");
            SetBrushColor("AccentPinkBrush", "#FB7185");
            SetBrushColor("AccentSoftBrush", "#123B46");
            SetBrushColor("SummaryPanelBrush", "#122033");
            SetBrushColor("SourcePanelBrush", "#0E1727");
            SetBrushColor("ChipNeutralBrush", "#26344D");
            SetBrushColor("ChipAccentBrush", "#173941");
            SetBrushColor("ChipAltBrush", "#223760");
            SetBrushColor("TextBrush", "#F8FAFC");
            SetBrushColor("MutedBrush", "#9FB3C8");
            SetBrushColor("WarningBrush", "#FBBF24");
            SetGradientColors("HeroGradientBrush", "#152742", "#0A1527", "#0B2231");
        }
    }

    private void SetBrushColor(string key, string hexColor)
    {
        Color color = (Color)ColorConverter.ConvertFromString(hexColor);

        if (_application.Resources[key] is SolidColorBrush brush)
        {
            if (brush.IsFrozen)
            {
                _application.Resources[key] = new SolidColorBrush(color);
            }
            else
            {
                var animation = new ColorAnimation
                {
                    To = color,
                    Duration = ThemeAnimationDuration,
                    EasingFunction = new QuadraticEase()
                };
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
        }
    }

    private void SetGradientColors(string key, string first, string second, string third)
    {
        if (_application.Resources[key] is LinearGradientBrush brush && brush.GradientStops.Count >= 3)
        {
            if (brush.IsFrozen)
            {
                var newBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };

                newBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(first), 0));
                newBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(second), 0.55));
                newBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(third), 1));
                _application.Resources[key] = newBrush;
            }
            else
            {
                AnimateGradientStop(brush.GradientStops[0], first);
                AnimateGradientStop(brush.GradientStops[1], second);
                AnimateGradientStop(brush.GradientStops[2], third);
            }
        }
    }

    private static void AnimateGradientStop(GradientStop stop, string hexColor)
    {
        Color target = (Color)ColorConverter.ConvertFromString(hexColor);
        var animation = new ColorAnimation
        {
            To = target,
            Duration = ThemeAnimationDuration,
            EasingFunction = new QuadraticEase()
        };
        stop.BeginAnimation(GradientStop.ColorProperty, animation);
    }
}
