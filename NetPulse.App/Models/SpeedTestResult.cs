namespace NetPulse.App.Models;

public sealed class SpeedTestResult
{
    public required double PingMs { get; init; }

    public required double JitterMs { get; init; }

    public required double DownloadMbps { get; init; }

    public required double UploadMbps { get; init; }

    public required string QualityLabel { get; init; }

    public required string ProviderName { get; init; }

    public required string IspName { get; init; }

    public required string ServerName { get; init; }

    public required string ServerLocation { get; init; }

    public string? ResultUrl { get; init; }
}
