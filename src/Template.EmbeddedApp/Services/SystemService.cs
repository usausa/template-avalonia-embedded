namespace Template.EmbeddedApp.Services;

using System.Diagnostics;

using LinuxDotNet.SystemInfo;

public sealed class SystemService : ISystemService
{
    private const ulong KiloByte = 1024;

    private const string ModelPath = "/proc/device-tree/model";

    private const string RootPath = "/";

    private const string LoopbackInterface = "lo";

    private readonly Lock sync = new();

    private readonly CpuCounter total = new();

    private CpuCounter[] cores = [];

    private SystemStat? systemStat;

    private MemoryStat? memoryStat;

    private LoadAverage? loadAverage;

    private Uptime? uptime;

    private FileSystemUsage? fileSystem;

    private NetworkStat? networkStat;

    private WirelessStat? wirelessStat;

    private ulong lastReceived;

    private ulong lastTransmitted;

    private long lastTimestamp;

    public bool IsSupported => OperatingSystem.IsLinux();

    public SystemInformation? ReadInformation()
    {
        if (!IsSupported)
        {
            return null;
        }

        var kernel = PlatformProvider.GetKernel();
        return new SystemInformation(ReadModel(), kernel.OsPrettyName ?? "-", kernel.OsRelease, Environment.MachineName, Environment.ProcessorCount);
    }

    public SystemSnapshot? Read()
    {
        if (!IsSupported)
        {
            return null;
        }

        lock (sync)
        {
            if ((systemStat is null) || (memoryStat is null) || (loadAverage is null) || (uptime is null))
            {
                systemStat = PlatformProvider.GetSystemStat();
                memoryStat = PlatformProvider.GetMemoryStat();
                loadAverage = PlatformProvider.GetLoadAverage();
                uptime = PlatformProvider.GetUptime();
                fileSystem = TryCreate(static () => PlatformProvider.GetFileSystemUsage(RootPath));
                networkStat = TryCreate(PlatformProvider.GetNetworkStat);
                wirelessStat = TryCreate(PlatformProvider.GetWirelessStat);
            }
            else
            {
                systemStat.Update();
                memoryStat.Update();
                loadAverage.Update();
                uptime.Update();
                fileSystem?.Update();
                networkStat?.Update();
                wirelessStat?.Update();
            }

            var usage = total.Update(systemStat.CpuTotal);
            if (cores.Length != systemStat.CpuCores.Count)
            {
                cores = systemStat.CpuCores.Select(static _ => new CpuCounter()).ToArray();
            }

            var coreUsages = new double[cores.Length];
            for (var i = 0; i < cores.Length; i++)
            {
                coreUsages[i] = cores[i].Update(systemStat.CpuCores[i]);
            }

            var (receiveRate, transmitRate) = CalculateNetworkRate();

            return new SystemSnapshot(
                usage,
                coreUsages,
                memoryStat.MemoryTotal * KiloByte,
                memoryStat.MemoryAvailable * KiloByte,
                loadAverage.Average1,
                loadAverage.Average5,
                loadAverage.Average15,
                uptime.Elapsed,
                fileSystem?.TotalSize ?? 0,
                fileSystem?.AvailableSize ?? 0,
                receiveRate,
                transmitRate,
                wirelessStat?.Interfaces.Count > 0 ? wirelessStat.Interfaces[0].SignalLevel : null);
        }
    }

    private (double Receive, double Transmit) CalculateNetworkRate()
    {
        if (networkStat is null)
        {
            return (0, 0);
        }

        var received = 0UL;
        var transmitted = 0UL;
        foreach (var entry in networkStat.Interfaces.Where(static x => x.Interface != LoopbackInterface))
        {
            received += entry.RxBytes;
            transmitted += entry.TxBytes;
        }

        var timestamp = Stopwatch.GetTimestamp();
        var seconds = lastTimestamp > 0 ? Stopwatch.GetElapsedTime(lastTimestamp, timestamp).TotalSeconds : 0;
        var rate = (seconds > 0) && (received >= lastReceived) && (transmitted >= lastTransmitted)
            ? ((received - lastReceived) / seconds, (transmitted - lastTransmitted) / seconds)
            : (0d, 0d);
        lastReceived = received;
        lastTransmitted = transmitted;
        lastTimestamp = timestamp;
        return rate;
    }

    private static string ReadModel()
    {
        if (File.Exists(ModelPath))
        {
            return File.ReadAllText(ModelPath).TrimEnd('\0', '\n');
        }

        var hardware = PlatformProvider.GetHardware();
        return String.IsNullOrEmpty(hardware.ProductName) ? "-" : hardware.ProductName;
    }

    private static T? TryCreate<T>(Func<T> factory)
        where T : class
    {
        try
        {
            return factory();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private sealed class CpuCounter
    {
        private ulong lastTotal;

        private ulong lastIdle;

        public double Update(CpuStat stat)
        {
            var idle = stat.Idle + stat.IoWait;
            var sum = stat.User + stat.Nice + stat.System + stat.Idle + stat.IoWait + stat.Irq + stat.SoftIrq + stat.Steal;
            var usage = (lastTotal > 0) && (sum > lastTotal) ? 100d * (1d - ((double)(idle - lastIdle) / (sum - lastTotal))) : 0d;
            lastTotal = sum;
            lastIdle = idle;
            return usage;
        }
    }
}
