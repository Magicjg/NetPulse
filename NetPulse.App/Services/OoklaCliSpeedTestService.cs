using System.Diagnostics;
using System.IO;
using System.Text.Json;
using NetPulse.App.Models;

namespace NetPulse.App.Services;

public sealed class OoklaCliSpeedTestService : ISpeedTestService
{
    private static readonly string[] CandidatePaths =
    [
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "WinGet",
            "Packages",
            "Ookla.Speedtest.CLI_Microsoft.Winget.Source_8wekyb3d8bbwe",
            "speedtest.exe"),
        "speedtest.exe"
    ];

    public async Task<SpeedTestResult> RunTestAsync(IProgress<SpeedTestProgress> progress, CancellationToken cancellationToken)
    {
        string executablePath = ResolveExecutablePath();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "--accept-license --accept-gdpr --progress=no --format=json-pretty",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        progress.Report(CreateProgress("Preparando Speedtest CLI", 6));

        process.Start();

        Task<string> stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        string[] phases =
        [
            "Buscando servidor optimo",
            "Midiendo ping y jitter",
            "Corriendo descarga real",
            "Corriendo subida real",
            "Procesando resultado final"
        ];

        double[] checkpoints = [14d, 28d, 56d, 82d, 96d];

        for (int index = 0; index < phases.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (process.HasExited)
            {
                break;
            }

            progress.Report(CreateProgress(phases[index], checkpoints[index]));
            await Task.Delay(index == 0 ? 700 : 1100, cancellationToken);
        }

        await process.WaitForExitAsync(cancellationToken);

        string stdOut = await stdOutTask;
        string stdErr = await stdErrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Speedtest CLI fallo con codigo {process.ExitCode}: {stdErr}".Trim());
        }

        string json = ExtractJson(stdOut);
        OoklaResultDto? dto = JsonSerializer.Deserialize<OoklaResultDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (dto is null || dto.Ping is null || dto.Download is null || dto.Upload is null || dto.Server is null)
        {
            throw new InvalidOperationException("No se pudo interpretar la respuesta de Speedtest CLI.");
        }

        double downloadMbps = ToMbps(dto.Download.Bandwidth);
        double uploadMbps = ToMbps(dto.Upload.Bandwidth);

        progress.Report(new SpeedTestProgress
        {
            Phase = "Resultado real obtenido",
            ProgressPercent = 100,
            PingMs = dto.Ping.Latency,
            JitterMs = dto.Ping.Jitter,
            DownloadMbps = downloadMbps,
            UploadMbps = uploadMbps,
            QualityLabel = CalculateQuality(dto.Ping.Latency, dto.Ping.Jitter, downloadMbps, uploadMbps)
        });

        return new SpeedTestResult
        {
            PingMs = dto.Ping.Latency,
            JitterMs = dto.Ping.Jitter,
            DownloadMbps = downloadMbps,
            UploadMbps = uploadMbps,
            QualityLabel = CalculateQuality(dto.Ping.Latency, dto.Ping.Jitter, downloadMbps, uploadMbps),
            ProviderName = "Ookla Speedtest CLI",
            IspName = dto.Isp ?? "ISP desconocido",
            ServerName = dto.Server.Name ?? "Servidor desconocido",
            ServerLocation = dto.Server.Location ?? "Ubicacion desconocida",
            ResultUrl = dto.Result?.Url
        };
    }

    private static SpeedTestProgress CreateProgress(string phase, double percent)
    {
        return new SpeedTestProgress
        {
            Phase = phase,
            ProgressPercent = percent,
            PingMs = 0,
            JitterMs = 0,
            DownloadMbps = 0,
            UploadMbps = 0,
            QualityLabel = "Midiendo"
        };
    }

    private static string ResolveExecutablePath()
    {
        foreach (string path in CandidatePaths)
        {
            if (Path.IsPathRooted(path))
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
            else
            {
                return path;
            }
        }

        throw new FileNotFoundException("No se encontro Speedtest CLI de Ookla. Instala Ookla.Speedtest.CLI.");
    }

    private static string ExtractJson(string output)
    {
        int start = output.IndexOf('{');
        int end = output.LastIndexOf('}');

        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("La salida de Speedtest CLI no contiene JSON valido.");
        }

        return output[start..(end + 1)];
    }

    private static double ToMbps(double bandwidthBytesPerSecond)
    {
        return Math.Round((bandwidthBytesPerSecond * 8d) / 1_000_000d, 2);
    }

    private static string CalculateQuality(double ping, double jitter, double download, double upload)
    {
        if (ping <= 20 && jitter <= 5 && download >= 150 && upload >= 10)
        {
            return "Excelente";
        }

        if (ping <= 35 && jitter <= 10 && download >= 80 && upload >= 8)
        {
            return "Muy buena";
        }

        if (ping <= 55 && download >= 35 && upload >= 4)
        {
            return "Estable";
        }

        return "Variable";
    }

    private sealed class OoklaResultDto
    {
        public OoklaPingDto? Ping { get; init; }

        public OoklaBandwidthDto? Download { get; init; }

        public OoklaBandwidthDto? Upload { get; init; }

        public string? Isp { get; init; }

        public OoklaServerDto? Server { get; init; }

        public OoklaResultLinkDto? Result { get; init; }
    }

    private sealed class OoklaPingDto
    {
        public double Jitter { get; init; }

        public double Latency { get; init; }
    }

    private sealed class OoklaBandwidthDto
    {
        public double Bandwidth { get; init; }
    }

    private sealed class OoklaServerDto
    {
        public string? Name { get; init; }

        public string? Location { get; init; }
    }

    private sealed class OoklaResultLinkDto
    {
        public string? Url { get; init; }
    }
}
