namespace Template.EmbeddedApp.Views.Main;

using Avalonia.Threading;

using RaspberryDotNet.SystemInfo;

using Template.EmbeddedApp.Services;
using Template.EmbeddedApp.State;

public sealed record CoreUsage(int Index, double Usage);

public sealed record SystemUsage(
    double CpuUsage,
    IReadOnlyList<CoreUsage> Cores,
    double MemoryUsage,
    double MemoryUsedMegabytes,
    double MemoryTotalMegabytes,
    double DiskUsage,
    double DiskUsedGigabytes,
    double DiskTotalGigabytes,
    double LoadAverage1,
    double LoadAverage5,
    double LoadAverage15,
    double ReceiveKilobytes,
    double TransmitKilobytes,
    double? SignalLevel);

public sealed partial class IndicatorItem : ObservableObject
{
    private readonly ThrottledFlags current;

    private readonly ThrottledFlags occurred;

    public string Name { get; }

    [ObservableProperty]
    public partial bool HasValue { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial bool HasOccurred { get; set; }

    public IndicatorItem(string name, ThrottledFlags current, ThrottledFlags occurred)
    {
        Name = name;
        this.current = current;
        this.occurred = occurred;
    }

    public void Update(ThrottledFlags flags)
    {
        HasValue = true;
        IsActive = (flags & current) != 0;
        HasOccurred = (flags & occurred) != 0;
    }
}

public sealed partial class DashboardViewModel : AppViewModelBase
{
    private const double KiloByte = 1024;

    private const double MegaByte = 1024 * 1024;

    private const double GigaByte = 1024 * 1024 * 1024;

    private const double MegaHertz = 1_000_000;

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);

    private readonly SystemService systemService;

    private readonly RaspberryService raspberryService;

    private readonly DispatcherTimer timer;

    public ObservableCollection<DeviceStatus> Devices { get; }

    public IReadOnlyList<IndicatorItem> Indicators { get; } =
    [
        new("Under-voltage", ThrottledFlags.UnderVoltageDetected, ThrottledFlags.UnderVoltageHasOccurred),
        new("Frequency capped", ThrottledFlags.ArmFrequencyCapped, ThrottledFlags.ArmFrequencyCappingHasOccurred),
        new("Throttled", ThrottledFlags.CurrentlyThrottled, ThrottledFlags.ThrottlingHasOccurred),
        new("Soft temp limit", ThrottledFlags.SoftTemperatureLimitActive, ThrottledFlags.SoftTemperatureLimitHasOccurred)
    ];

    public bool IsSystemSupported => systemService.IsSupported;

    public bool IsRaspberrySupported => raspberryService.IsSupported;

    public SystemInformation? Information { get; }

    public string? Model => Information?.Model;

    [ObservableProperty]
    public partial TimeSpan? Uptime { get; set; }

    [ObservableProperty]
    public partial double? Temperature { get; set; }

    [ObservableProperty]
    public partial double? ArmClockMegahertz { get; set; }

    [ObservableProperty]
    public partial double? CoreClockMegahertz { get; set; }

    [ObservableProperty]
    public partial double? CoreVoltage { get; set; }

    [ObservableProperty]
    public partial SystemUsage? SystemUsage { get; set; }

    public DashboardViewModel(DeviceState deviceState, SystemService systemService, RaspberryService raspberryService)
    {
        this.systemService = systemService;
        this.raspberryService = raspberryService;
        Devices = deviceState.Devices;
        Information = systemService.ReadInformation();

        timer = new DispatcherTimer { Interval = RefreshInterval };
        timer.Tick += (_, _) => Refresh();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Stop();
        }

        base.Dispose(disposing);
    }

    public override void OnNavigatedTo(INavigationContext context)
    {
        Refresh();
        timer.Start();
    }

    public override void OnNavigatingFrom(INavigationContext context)
    {
        timer.Stop();
    }

    private void Refresh()
    {
        RefreshRaspberry();
        RefreshSystem();
    }

    private void RefreshRaspberry()
    {
        if (raspberryService.Read() is not { } snapshot)
        {
            return;
        }

        Temperature = snapshot.Temperature;
        ArmClockMegahertz = snapshot.ArmClock / MegaHertz;
        CoreClockMegahertz = snapshot.CoreClock / MegaHertz;
        CoreVoltage = snapshot.CoreVoltage;
        foreach (var indicator in Indicators)
        {
            indicator.Update(snapshot.Throttled);
        }
    }

    private void RefreshSystem()
    {
        if (systemService.Read() is not { } snapshot)
        {
            return;
        }

        var memoryUsed = snapshot.MemoryTotal - snapshot.MemoryAvailable;
        var diskUsed = snapshot.DiskTotal - snapshot.DiskAvailable;
        Uptime = snapshot.Uptime;
        SystemUsage = new SystemUsage(
            snapshot.CpuUsage,
            [.. snapshot.CoreUsages.Select(static (x, i) => new CoreUsage(i, x))],
            snapshot.MemoryTotal > 0 ? 100d * memoryUsed / snapshot.MemoryTotal : 0,
            memoryUsed / MegaByte,
            snapshot.MemoryTotal / MegaByte,
            snapshot.DiskTotal > 0 ? 100d * diskUsed / snapshot.DiskTotal : 0,
            diskUsed / GigaByte,
            snapshot.DiskTotal / GigaByte,
            snapshot.LoadAverage1,
            snapshot.LoadAverage5,
            snapshot.LoadAverage15,
            snapshot.ReceiveRate / KiloByte,
            snapshot.TransmitRate / KiloByte,
            snapshot.SignalLevel);
    }
}
