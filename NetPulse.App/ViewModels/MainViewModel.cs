using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NetPulse.App.Models;
using NetPulse.App.Services;

namespace NetPulse.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ISpeedTestService _speedTestService;
    private readonly HistoryPersistenceService _historyPersistenceService;
    private readonly ThemeService _themeService;
    private CancellationTokenSource? _testCancellation;
    private bool _isTesting;
    private string _currentPhase;
    private string _statusText;
    private double _progressPercent;
    private double _pingValue;
    private double _jitterValue;
    private double _downloadValue;
    private double _uploadValue;
    private string _qualityLabel;
    private string _heroBadgeText;
    private string _providerText;
    private string _serverText;
    private string _ispText;
    private string _resultLinkText;
    private string? _currentResultUrl;
    private Brush _progressAccentBrush;
    private Brush _qualityAccentBrush;
    private readonly DispatcherTimer _metricAnimationTimer;
    private double _animatedPingValue;
    private double _animatedJitterValue;
    private double _animatedDownloadValue;
    private double _animatedUploadValue;

    public MainViewModel()
        : this(new OoklaCliSpeedTestService(), new HistoryPersistenceService(), new ThemeService(System.Windows.Application.Current, new AppSettingsService()))
    {
    }

    public MainViewModel(
        ISpeedTestService speedTestService,
        HistoryPersistenceService historyPersistenceService,
        ThemeService themeService)
    {
        _speedTestService = speedTestService;
        _historyPersistenceService = historyPersistenceService;
        _themeService = themeService;
        _currentPhase = "Listo para correr una prueba";
        _statusText = "Listo para medir con un backend real basado en Ookla Speedtest CLI.";
        _qualityLabel = "En espera";
        _heroBadgeText = "Dashboard activo";
        _providerText = "Fuente pendiente";
        _serverText = "Servidor pendiente";
        _ispText = "ISP pendiente";
        _resultLinkText = "Sin enlace de resultado todavia";
        _progressAccentBrush = CreateBrush("#4F8CFF");
        _qualityAccentBrush = CreateBrush("#FB7185");
        _metricAnimationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(28)
        };
        _metricAnimationTimer.Tick += OnMetricAnimationTick;

        Metrics =
        [
            new DashboardMetric
            {
                Label = "Ping",
                Value = "--",
                Description = "Latencia actual"
            },
            new DashboardMetric
            {
                Label = "Download",
                Value = "--",
                Description = "Velocidad de bajada"
            },
            new DashboardMetric
            {
                Label = "Upload",
                Value = "--",
                Description = "Velocidad de subida"
            }
        ];

        History = new ObservableCollection<HistoryItem>(_historyPersistenceService.Load());

        StartTestCommand = new RelayCommand(StartTest, () => !IsTesting);
        CancelTestCommand = new RelayCommand(CancelTest, () => IsTesting);
        OpenResultCommand = new RelayCommand(OpenResultLink, () => !string.IsNullOrWhiteSpace(CurrentResultUrl));
        ClearHistoryCommand = new RelayCommand(ClearHistory, () => History.Count > 0);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);

        OnPropertyChanged(nameof(HistoryCountText));
        OnPropertyChanged(nameof(LatestHistoryText));
        OnPropertyChanged(nameof(BestDownloadText));
        OnPropertyChanged(nameof(HistoryEmptyText));
        OnPropertyChanged(nameof(ThemeButtonText));
        OnPropertyChanged(nameof(ThemeDescriptionText));
    }

    public string AppTitle => "NetPulse";

    public string AppSubtitle => "Medidor de velocidad de internet para Windows con una interfaz visual fuerte, amigable y lista para animaciones reales.";

    public ObservableCollection<DashboardMetric> Metrics { get; }

    public ObservableCollection<HistoryItem> History { get; }

    public ICommand StartTestCommand { get; }

    public ICommand CancelTestCommand { get; }

    public ICommand OpenResultCommand { get; }

    public ICommand ClearHistoryCommand { get; }

    public ICommand ToggleThemeCommand { get; }

    public bool IsTesting
    {
        get => _isTesting;
        private set
        {
            if (SetProperty(ref _isTesting, value))
            {
                RaiseCommandStates();
                OnPropertyChanged(nameof(StartButtonText));
            }
        }
    }

    public string StartButtonText => IsTesting ? "Midiendo..." : "Iniciar prueba";

    public string CurrentPhase
    {
        get => _currentPhase;
        private set => SetProperty(ref _currentPhase, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string HeroBadgeText
    {
        get => _heroBadgeText;
        private set => SetProperty(ref _heroBadgeText, value);
    }

    public string QualityLabel
    {
        get => _qualityLabel;
        private set => SetProperty(ref _qualityLabel, value);
    }

    public string ProviderText
    {
        get => _providerText;
        private set => SetProperty(ref _providerText, value);
    }

    public string ServerText
    {
        get => _serverText;
        private set => SetProperty(ref _serverText, value);
    }

    public string IspText
    {
        get => _ispText;
        private set => SetProperty(ref _ispText, value);
    }

    public string ResultLinkText
    {
        get => _resultLinkText;
        private set => SetProperty(ref _resultLinkText, value);
    }

    public string? CurrentResultUrl
    {
        get => _currentResultUrl;
        private set
        {
            if (SetProperty(ref _currentResultUrl, value))
            {
                ((RelayCommand)OpenResultCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string HistoryCountText => $"{History.Count}";

    public string LatestHistoryText => History.Count > 0 ? History[0].DownloadValue.ToString("0.0") : "--";

    public string BestDownloadText => History.Count > 0
        ? $"{History.Max(item => item.DownloadValue):0.0}"
        : "--";

    public string HistoryEmptyText => History.Count == 0
        ? "Todavia no hay pruebas guardadas. Corre una medicion para llenar este panel."
        : string.Empty;

    public string ThemeButtonText => _themeService.ThemeButtonText;

    public string ThemeDescriptionText => _themeService.ThemeDescriptionText;

    public bool IsDarkTheme
    {
        get => _themeService.IsDarkTheme;
        set
        {
            if (value == _themeService.IsDarkTheme)
            {
                return;
            }

            _themeService.SetTheme(value);
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(ThemeButtonText));
            OnPropertyChanged(nameof(ThemeDescriptionText));
        }
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        private set
        {
            if (SetProperty(ref _progressPercent, value))
            {
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public string ProgressText => $"{ProgressPercent:0}% completado";

    public Brush ProgressAccentBrush
    {
        get => _progressAccentBrush;
        private set => SetProperty(ref _progressAccentBrush, value);
    }

    public Brush QualityAccentBrush
    {
        get => _qualityAccentBrush;
        private set => SetProperty(ref _qualityAccentBrush, value);
    }

    public double PingValue
    {
        get => _pingValue;
        private set
        {
            if (SetProperty(ref _pingValue, value))
            {
                Metrics[0].Value = value > 0 ? $"{value:0.0}" : "--";
                Metrics[0].Description = "Latencia en tiempo real";
                RefreshMetrics();
                OnPropertyChanged(nameof(PingText));
                OnPropertyChanged(nameof(PingValueDisplay));
            }
        }
    }

    public double JitterValue
    {
        get => _jitterValue;
        private set
        {
            if (SetProperty(ref _jitterValue, value))
            {
                OnPropertyChanged(nameof(JitterText));
                OnPropertyChanged(nameof(JitterValueDisplay));
            }
        }
    }

    public double DownloadValue
    {
        get => _downloadValue;
        private set
        {
            if (SetProperty(ref _downloadValue, value))
            {
                Metrics[1].Value = value > 0 ? $"{value:0.0}" : "--";
                Metrics[1].Description = "Descarga instantanea";
                RefreshMetrics();
                OnPropertyChanged(nameof(DownloadText));
                OnPropertyChanged(nameof(DownloadValueDisplay));
            }
        }
    }

    public double UploadValue
    {
        get => _uploadValue;
        private set
        {
            if (SetProperty(ref _uploadValue, value))
            {
                Metrics[2].Value = value > 0 ? $"{value:0.0}" : "--";
                Metrics[2].Description = "Subida instantanea";
                RefreshMetrics();
                OnPropertyChanged(nameof(UploadText));
                OnPropertyChanged(nameof(UploadValueDisplay));
            }
        }
    }

    public string PingText => $"{PingValue:0.0} ms";

    public string JitterText => $"{JitterValue:0.0} ms";

    public string DownloadText => $"{DownloadValue:0.0} Mbps";

    public string UploadText => $"{UploadValue:0.0} Mbps";

    public string DownloadValueDisplay => DownloadValue > 0 ? $"{DownloadValue:0.0}" : "--";

    public string UploadValueDisplay => UploadValue > 0 ? $"{UploadValue:0.0}" : "--";

    public string PingValueDisplay => PingValue > 0 ? $"{PingValue:0.0}" : "--";

    public string JitterValueDisplay => JitterValue > 0 ? $"{JitterValue:0.0}" : "--";

    public string AnimatedDownloadValueDisplay => GetAnimatedDisplay(_animatedDownloadValue, DownloadValue);

    public string AnimatedUploadValueDisplay => GetAnimatedDisplay(_animatedUploadValue, UploadValue);

    public string AnimatedPingValueDisplay => GetAnimatedDisplay(_animatedPingValue, PingValue);

    public string AnimatedJitterValueDisplay => GetAnimatedDisplay(_animatedJitterValue, JitterValue);

    private async void StartTest()
    {
        if (IsTesting)
        {
            return;
        }

        IsTesting = true;
        HeroBadgeText = "Prueba en curso";
        StatusText = "Iniciando medicion real con Ookla Speedtest CLI.";
        CurrentPhase = "Preparando prueba";
        ProgressPercent = 0;
        ResetAnimatedMetrics();
        UpdateProgressVisualState(CurrentPhase);

        _testCancellation = new CancellationTokenSource();
        var progress = new Progress<SpeedTestProgress>(UpdateFromProgress);

        try
        {
            SpeedTestResult result = await _speedTestService.RunTestAsync(progress, _testCancellation.Token);
            ApplyResult(result);
            AddHistory(result);
            HeroBadgeText = "Prueba terminada";
            StatusText = "Resultado real obtenido correctamente.";
            CurrentPhase = "Prueba completada";
            ProgressPercent = 100;
            UpdateProgressVisualState(CurrentPhase);
        }
        catch (OperationCanceledException)
        {
            HeroBadgeText = "Prueba cancelada";
            StatusText = "Cancelaste la medicion antes de terminar.";
            CurrentPhase = "En pausa";
            UpdateProgressVisualState(CurrentPhase);
        }
        catch (FileNotFoundException)
        {
            HeroBadgeText = "Falta Speedtest CLI";
            StatusText = "No se encontro Ookla Speedtest CLI. Instalala con winget: winget install Ookla.Speedtest.CLI";
            CurrentPhase = "Herramienta faltante";
            UpdateProgressVisualState(CurrentPhase);
        }
        catch (Exception ex)
        {
            HeroBadgeText = "Error en la prueba";
            StatusText = $"No se pudo completar la medicion real: {ex.Message}";
            CurrentPhase = "Error";
            UpdateProgressVisualState(CurrentPhase);
        }
        finally
        {
            _testCancellation?.Dispose();
            _testCancellation = null;
            IsTesting = false;
        }
    }

    private void CancelTest()
    {
        _testCancellation?.Cancel();
    }

    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(ThemeButtonText));
        OnPropertyChanged(nameof(ThemeDescriptionText));
    }

    private void ClearHistory()
    {
        History.Clear();
        _historyPersistenceService.Save(History);
        OnPropertyChanged(nameof(HistoryCountText));
        OnPropertyChanged(nameof(LatestHistoryText));
        OnPropertyChanged(nameof(BestDownloadText));
        OnPropertyChanged(nameof(HistoryEmptyText));
        RaiseCommandStates();
    }

    private void UpdateFromProgress(SpeedTestProgress progress)
    {
        CurrentPhase = progress.Phase;
        ProgressPercent = progress.ProgressPercent;
        PingValue = progress.PingMs;
        JitterValue = progress.JitterMs;
        DownloadValue = progress.DownloadMbps;
        UploadValue = progress.UploadMbps;
        EnsureMetricAnimationRunning();
        QualityLabel = progress.QualityLabel;
        StatusText = $"Midiendo red: {progress.Phase}. Calidad estimada: {progress.QualityLabel}.";
        UpdateProgressVisualState(progress.Phase);
    }

    private void ApplyResult(SpeedTestResult result)
    {
        PingValue = result.PingMs;
        JitterValue = result.JitterMs;
        DownloadValue = result.DownloadMbps;
        UploadValue = result.UploadMbps;
        QualityLabel = result.QualityLabel;
        ProviderText = $"Motor: {result.ProviderName}";
        IspText = $"ISP: {result.IspName}";
        ServerText = $"Servidor: {result.ServerName} | {result.ServerLocation}";
        CurrentResultUrl = result.ResultUrl;
        ResultLinkText = string.IsNullOrWhiteSpace(result.ResultUrl)
            ? "Sin enlace publico para esta prueba"
            : "Abrir resultado de Ookla en navegador";
    }

    private void AddHistory(SpeedTestResult result)
    {
        History.Insert(0, new HistoryItem
        {
            Title = $"Sesion {DateTime.Now:HH:mm}",
            Summary = $"Calidad {result.QualityLabel} con jitter {result.JitterMs:0.0} ms.",
            DetailText = $"{result.IspName} | {result.ServerName} | {result.ServerLocation}",
            DownloadText = $"Down {result.DownloadMbps:0.0} Mbps",
            UploadText = $"Up {result.UploadMbps:0.0} Mbps",
            PingText = $"Ping {result.PingMs:0.0} ms",
            QualityText = result.QualityLabel,
            TimestampText = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            DownloadValue = result.DownloadMbps,
            QualityStripeBrush = GetQualityStripeBrush(result.QualityLabel)
        });

        _historyPersistenceService.Save(History);
        OnPropertyChanged(nameof(HistoryCountText));
        OnPropertyChanged(nameof(LatestHistoryText));
        OnPropertyChanged(nameof(BestDownloadText));
        OnPropertyChanged(nameof(HistoryEmptyText));
        RaiseCommandStates();
    }

    private static string GetQualityStripeBrush(string qualityLabel)
    {
        return qualityLabel switch
        {
            "Excelente" => "#22C55E",
            "Muy buena" => "#2DD4BF",
            "Estable" => "#F59E0B",
            "Variable" => "#F97316",
            _ => "#64748B"
        };
    }

    private void OpenResultLink()
    {
        if (string.IsNullOrWhiteSpace(CurrentResultUrl))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = CurrentResultUrl,
            UseShellExecute = true
        });
    }

    private void RefreshMetrics()
    {
        OnPropertyChanged(nameof(Metrics));
    }

    private void EnsureMetricAnimationRunning()
    {
        if (!_metricAnimationTimer.IsEnabled)
        {
            _metricAnimationTimer.Start();
        }
    }

    private void OnMetricAnimationTick(object? sender, EventArgs e)
    {
        bool changed = false;

        changed |= AnimateMetric(ref _animatedPingValue, PingValue);
        changed |= AnimateMetric(ref _animatedJitterValue, JitterValue);
        changed |= AnimateMetric(ref _animatedDownloadValue, DownloadValue);
        changed |= AnimateMetric(ref _animatedUploadValue, UploadValue);

        if (changed)
        {
            OnPropertyChanged(nameof(AnimatedPingValueDisplay));
            OnPropertyChanged(nameof(AnimatedJitterValueDisplay));
            OnPropertyChanged(nameof(AnimatedDownloadValueDisplay));
            OnPropertyChanged(nameof(AnimatedUploadValueDisplay));
        }

        if (AreClose(_animatedPingValue, PingValue) &&
            AreClose(_animatedJitterValue, JitterValue) &&
            AreClose(_animatedDownloadValue, DownloadValue) &&
            AreClose(_animatedUploadValue, UploadValue))
        {
            _metricAnimationTimer.Stop();
        }
    }

    private void ResetAnimatedMetrics()
    {
        _metricAnimationTimer.Stop();
        _animatedPingValue = 0;
        _animatedJitterValue = 0;
        _animatedDownloadValue = 0;
        _animatedUploadValue = 0;
        OnPropertyChanged(nameof(AnimatedPingValueDisplay));
        OnPropertyChanged(nameof(AnimatedJitterValueDisplay));
        OnPropertyChanged(nameof(AnimatedDownloadValueDisplay));
        OnPropertyChanged(nameof(AnimatedUploadValueDisplay));
    }

    private static bool AnimateMetric(ref double current, double target)
    {
        if (AreClose(current, target))
        {
            current = target;
            return false;
        }

        double distance = target - current;
        double step = Math.Max(Math.Abs(distance) * 0.18, 0.2);
        current += Math.Sign(distance) * step;

        if ((distance > 0 && current > target) || (distance < 0 && current < target))
        {
            current = target;
        }

        return true;
    }

    private static bool AreClose(double left, double right)
    {
        return Math.Abs(left - right) < 0.05;
    }

    private static string GetAnimatedDisplay(double animatedValue, double realValue)
    {
        return realValue > 0 || animatedValue > 0 ? $"{Math.Max(animatedValue, 0):0.0}" : "--";
    }

    private void UpdateProgressVisualState(string phase)
    {
        ProgressAccentBrush = CreateBrush(phase switch
        {
            var text when text.Contains("Prepar", StringComparison.OrdinalIgnoreCase) => "#4F8CFF",
            var text when text.Contains("Buscando", StringComparison.OrdinalIgnoreCase) => "#4F8CFF",
            var text when text.Contains("ping", StringComparison.OrdinalIgnoreCase) => "#F59E0B",
            var text when text.Contains("descarga", StringComparison.OrdinalIgnoreCase) => "#2DD4BF",
            var text when text.Contains("subida", StringComparison.OrdinalIgnoreCase) => "#8B5CF6",
            var text when text.Contains("resultado", StringComparison.OrdinalIgnoreCase) => "#22C55E",
            var text when text.Contains("completada", StringComparison.OrdinalIgnoreCase) => "#22C55E",
            var text when text.Contains("pausa", StringComparison.OrdinalIgnoreCase) => "#F59E0B",
            var text when text.Contains("faltante", StringComparison.OrdinalIgnoreCase) => "#F97316",
            var text when text.Contains("error", StringComparison.OrdinalIgnoreCase) => "#FB7185",
            _ => "#4F8CFF"
        });

        QualityAccentBrush = CreateBrush(QualityLabel switch
        {
            "Excelente" => "#22C55E",
            "Muy buena" => "#2DD4BF",
            "Estable" => "#F59E0B",
            "Variable" => "#F97316",
            "En espera" => "#FB7185",
            "Midiendo" => "#4F8CFF",
            _ => "#94A3B8"
        });
    }

    private static Brush CreateBrush(string colorHex)
    {
        Brush brush = (Brush)new BrushConverter().ConvertFrom(colorHex)!;
        brush.Freeze();
        return brush;
    }

    private void RaiseCommandStates()
    {
        ((RelayCommand)StartTestCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CancelTestCommand).RaiseCanExecuteChanged();
        ((RelayCommand)OpenResultCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ClearHistoryCommand).RaiseCanExecuteChanged();
    }
}
