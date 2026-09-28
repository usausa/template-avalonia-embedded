namespace Template.EmbeddedApp.Views.Main;

using Avalonia.Threading;

using Template.EmbeddedApp.Devices.Platform;

public sealed partial class MonitorViewModel : AppViewModelBase
{
    public const int HistoryLength = 60;

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);

    private readonly TimeProvider timeProvider;

    private readonly ISystemMonitor systemMonitor;

    private readonly IRaspberryMonitor raspberryMonitor;

    private readonly DispatcherTimer timer;

    private readonly Queue<double> cpuHistory = new();

    private readonly Queue<double> temperatureHistory = new();

    private readonly Queue<double> memoryHistory = new();

    private readonly long startTimestamp;

    public bool IsDemo { get; }

    [ObservableProperty]
    public partial double Cpu { get; set; }

    [ObservableProperty]
    public partial string CpuText { get; set; } = "--";

    [ObservableProperty]
    public partial double Temperature { get; set; }

    [ObservableProperty]
    public partial string TemperatureText { get; set; } = "--.-";

    [ObservableProperty]
    public partial double Memory { get; set; }

    [ObservableProperty]
    public partial string MemoryText { get; set; } = "--";

    [ObservableProperty]
    public partial IReadOnlyList<double> CpuValues { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> TemperatureValues { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> MemoryValues { get; set; } = [];

    public MonitorViewModel(TimeProvider timeProvider, ISystemMonitor systemMonitor, IRaspberryMonitor raspberryMonitor)
    {
        this.timeProvider = timeProvider;
        this.systemMonitor = systemMonitor;
        this.raspberryMonitor = raspberryMonitor;
        IsDemo = !systemMonitor.IsSupported;
        startTimestamp = timeProvider.GetTimestamp();

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
        double cpu;
        double memory;
        double? temperature;
        if (IsDemo)
        {
            var seconds = timeProvider.GetElapsedTime(startTimestamp).TotalSeconds;
            cpu = 50 + (40 * Math.Sin(seconds / 5));
            memory = 40 + (10 * Math.Sin(seconds / 11));
            temperature = 50 + (10 * Math.Sin(seconds / 7));
        }
        else
        {
            var snapshot = systemMonitor.Read();
            cpu = snapshot?.CpuUsage ?? 0;
            memory = snapshot is { MemoryTotal: > 0 } ? 100d * (snapshot.MemoryTotal - snapshot.MemoryAvailable) / snapshot.MemoryTotal : 0;
            temperature = raspberryMonitor.Read()?.Temperature;
        }

        var culture = CultureInfo.InvariantCulture;
        Cpu = cpu;
        CpuText = String.Create(culture, $"{cpu:F0}");
        Memory = memory;
        MemoryText = String.Create(culture, $"{memory:F0}");
        Temperature = temperature ?? 0;
        TemperatureText = temperature is { } value ? String.Create(culture, $"{value:F1}") : "--.-";
        CpuValues = Push(cpuHistory, cpu);
        MemoryValues = Push(memoryHistory, memory);
        TemperatureValues = temperature is { } current ? Push(temperatureHistory, current) : TemperatureValues;
    }

    private static double[] Push(Queue<double> history, double value)
    {
        history.Enqueue(value);
        while (history.Count > HistoryLength)
        {
            history.Dequeue();
        }

        return history.ToArray();
    }
}
