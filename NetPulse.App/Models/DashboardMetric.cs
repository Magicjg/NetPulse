using NetPulse.App.ViewModels;

namespace NetPulse.App.Models;

public sealed class DashboardMetric : ObservableObject
{
    private string _value = string.Empty;
    private string _description = string.Empty;

    public required string Label { get; init; }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
}
