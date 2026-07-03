using NetPulse.App.Models;

namespace NetPulse.App.Services;

public interface ISpeedTestService
{
    Task<SpeedTestResult> RunTestAsync(IProgress<SpeedTestProgress> progress, CancellationToken cancellationToken);
}
