namespace NetPulse.App.Models;

public sealed class SpeedTestProgress
{
    public required string Phase { get; init; }

    public required double ProgressPercent { get; init; }

    public required double PingMs { get; init; }

    public required double JitterMs { get; init; }

    public required double DownloadMbps { get; init; }

    public required double UploadMbps { get; init; }

    public required string QualityLabel { get; init; }
}
