namespace NetPulse.App.Models;

public sealed class HistoryItem
{
    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required string DetailText { get; init; }

    public required string DownloadText { get; init; }

    public required string UploadText { get; init; }

    public required string PingText { get; init; }

    public required string QualityText { get; init; }

    public required string TimestampText { get; init; }

    public required double DownloadValue { get; init; }

    public required string QualityStripeBrush { get; init; }
}
