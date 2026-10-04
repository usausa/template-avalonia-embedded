namespace Template.EmbeddedApp.Services;

public sealed record SystemInformation(string Model, string OperatingSystem, string Kernel, string HostName, int CpuCount);

public sealed record SystemSnapshot(
    double CpuUsage,
    IReadOnlyList<double> CoreUsages,
    ulong MemoryTotal,
    ulong MemoryAvailable,
    double LoadAverage1,
    double LoadAverage5,
    double LoadAverage15,
    TimeSpan Uptime,
    ulong DiskTotal,
    ulong DiskAvailable,
    double ReceiveRate,
    double TransmitRate,
    double? SignalLevel);

public interface ISystemService
{
    bool IsSupported { get; }

    SystemInformation? ReadInformation();

    SystemSnapshot? Read();
}
