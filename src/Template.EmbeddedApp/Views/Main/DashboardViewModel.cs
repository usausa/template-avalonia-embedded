namespace Template.EmbeddedApp.Views.Main;

using Avalonia.Threading;

using RaspberryDotNet.SystemInfo;

using Template.EmbeddedApp.Devices.Platform;
using Template.EmbeddedApp.State;

public sealed partial class CoreItem : ObservableObject
{
    public string Name { get; }

    [ObservableProperty]
    public partial double Usage { get; set; }

    public CoreItem(string name)
    {
        Name = name;
    }
}

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

    private readonly ISystemMonitor systemMonitor;

    private readonly IRaspberryMonitor raspberryMonitor;

    private readonly DispatcherTimer timer;

    public ObservableCollection<DeviceStatus> Devices { get; }

    public ObservableCollection<CoreItem> Cores { get; } = [];

    public IReadOnlyList<IndicatorItem> Indicators { get; } =
    [
        new("Under-voltage", ThrottledFlags.UnderVoltageDetected, ThrottledFlags.UnderVoltageHasOccurred),
        new("Frequency capped", ThrottledFlags.ArmFrequencyCapped, ThrottledFlags.ArmFrequencyCappingHasOccurred),
        new("Throttled", ThrottledFlags.CurrentlyThrottled, ThrottledFlags.ThrottlingHasOccurred),
        new("Soft temp limit", ThrottledFlags.SoftTemperatureLimitActive, ThrottledFlags.SoftTemperatureLimitHasOccurred)
    ];

    public bool IsSystemSupported => systemMonitor.IsSupported;

    public bool IsRaspberrySupported => raspberryMonitor.IsSupported;

    public string Model { get; }

    public string Platform { get; }

    [ObservableProperty]
    public partial string Uptime { get; set; } = "-";

    [ObservableProperty]
    public partial string Temperature { get; set; } = "--.-";

    [ObservableProperty]
    public partial string ArmClock { get; set; } = "-";

    [ObservableProperty]
    public partial string CoreClock { get; set; } = "-";

    [ObservableProperty]
    public partial string CoreVoltage { get; set; } = "-";

    [ObservableProperty]
    public partial string Cpu { get; set; } = "-";

    [ObservableProperty]
    public partial double MemoryUsage { get; set; }

    [ObservableProperty]
    public partial string Memory { get; set; } = "-";

    [ObservableProperty]
    public partial double DiskUsage { get; set; }

    [ObservableProperty]
    public partial string Disk { get; set; } = "-";

    [ObservableProperty]
    public partial string LoadAverage { get; set; } = "-";

    [ObservableProperty]
    public partial string Network { get; set; } = "-";

    [ObservableProperty]
    public partial string Signal { get; set; } = "-";

    public DashboardViewModel(DeviceState deviceState, ISystemMonitor systemMonitor, IRaspberryMonitor raspberryMonitor)
    {
        this.systemMonitor = systemMonitor;
        this.raspberryMonitor = raspberryMonitor;
        Devices = deviceState.Devices;

        var information = systemMonitor.ReadInformation();
        Model = information?.Model ?? "-";
        Platform = information is null
            ? "-"
            : String.Create(CultureInfo.InvariantCulture, $"{information.HostName}  {information.OperatingSystem}  {information.Kernel}");

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
        if (raspberryMonitor.Read() is not { } snapshot)
        {
            return;
        }

        var culture = CultureInfo.InvariantCulture;
        Temperature = String.Create(culture, $"{snapshot.Temperature:F1}");
        ArmClock = String.Create(culture, $"{snapshot.ArmClock / MegaHertz:F0} MHz");
        CoreClock = String.Create(culture, $"{snapshot.CoreClock / MegaHertz:F0} MHz");
        CoreVoltage = String.Create(culture, $"{snapshot.CoreVoltage:F3} V");
        foreach (var indicator in Indicators)
        {
            indicator.Update(snapshot.Throttled);
        }
    }

    private void RefreshSystem()
    {
        if (systemMonitor.Read() is not { } snapshot)
        {
            return;
        }

        var culture = CultureInfo.InvariantCulture;
        Uptime = snapshot.Uptime.ToString(@"d\.hh\:mm\:ss", culture);
        Cpu = String.Create(culture, $"{snapshot.CpuUsage:F1} %");
        if (Cores.Count != snapshot.CoreUsages.Count)
        {
            Cores.Clear();
            for (var i = 0; i < snapshot.CoreUsages.Count; i++)
            {
                Cores.Add(new CoreItem(String.Create(culture, $"CPU{i}")));
            }
        }

        for (var i = 0; i < Cores.Count; i++)
        {
            Cores[i].Usage = snapshot.CoreUsages[i];
        }

        var memoryUsed = snapshot.MemoryTotal - snapshot.MemoryAvailable;
        MemoryUsage = snapshot.MemoryTotal > 0 ? 100d * memoryUsed / snapshot.MemoryTotal : 0;
        Memory = String.Create(culture, $"{memoryUsed / MegaByte:F0} / {snapshot.MemoryTotal / MegaByte:F0} MB");

        var diskUsed = snapshot.DiskTotal - snapshot.DiskAvailable;
        DiskUsage = snapshot.DiskTotal > 0 ? 100d * diskUsed / snapshot.DiskTotal : 0;
        Disk = String.Create(culture, $"{diskUsed / GigaByte:F1} / {snapshot.DiskTotal / GigaByte:F1} GB");

        LoadAverage = String.Create(culture, $"{snapshot.LoadAverage1:F2}  {snapshot.LoadAverage5:F2}  {snapshot.LoadAverage15:F2}");
        Network = String.Create(culture, $"RX {snapshot.ReceiveRate / KiloByte:F1}  TX {snapshot.TransmitRate / KiloByte:F1} KB/s");
        Signal = snapshot.SignalLevel is { } level ? String.Create(culture, $"{level:F0} dBm") : "-";
    }
}
