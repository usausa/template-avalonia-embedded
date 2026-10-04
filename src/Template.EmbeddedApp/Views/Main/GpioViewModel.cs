namespace Template.EmbeddedApp.Views.Main;

using Avalonia.Threading;

using RaspberryDotNet.SystemInfo;

using Template.EmbeddedApp.Services;

public sealed partial class GpioPinItem : ObservableObject
{
    public int PhysicalPin { get; }

    public string Name { get; }

    public int? SocPin { get; }

    public bool IsPower3V3 => Name == "3.3V";

    public bool IsPower5V => Name == "5V";

    public bool IsGround => Name == "GND";

    [ObservableProperty]
    public partial GpioFunction? Function { get; set; }

    [ObservableProperty]
    public partial bool IsHigh { get; set; }

    public GpioPinItem(int physicalPin, string name, int? socPin)
    {
        PhysicalPin = physicalPin;
        Name = name;
        SocPin = socPin;
    }
}

public sealed partial class GpioViewModel : AppViewModelBase
{
    private const int PageSize = 20;

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(500);

    private static readonly (string Name, int? SocPin)[] Header =
    [
        ("3.3V", null), ("5V", null),
        ("GPIO2 SDA1", 2), ("5V", null),
        ("GPIO3 SCL1", 3), ("GND", null),
        ("GPIO4", 4), ("GPIO14 TXD", 14),
        ("GND", null), ("GPIO15 RXD", 15),
        ("GPIO17", 17), ("GPIO18 PWM0", 18),
        ("GPIO27", 27), ("GND", null),
        ("GPIO22", 22), ("GPIO23", 23),
        ("3.3V", null), ("GPIO24", 24),
        ("GPIO10 MOSI", 10), ("GND", null),
        ("GPIO9 MISO", 9), ("GPIO25", 25),
        ("GPIO11 SCLK", 11), ("GPIO8 CE0", 8),
        ("GND", null), ("GPIO7 CE1", 7),
        ("GPIO0 ID_SD", 0), ("GPIO1 ID_SC", 1),
        ("GPIO5", 5), ("GND", null),
        ("GPIO6", 6), ("GPIO12 PWM0", 12),
        ("GPIO13 PWM1", 13), ("GND", null),
        ("GPIO19 PWM1", 19), ("GPIO16", 16),
        ("GPIO26", 26), ("GPIO20", 20),
        ("GND", null), ("GPIO21", 21)
    ];

    private readonly IRaspberryService raspberryService;

    private readonly DispatcherTimer timer;

    private int page;

    public IReadOnlyList<GpioPinItem> Pins { get; }

    public bool IsSupported => raspberryService.IsGpioSupported;

    [ObservableProperty]
    public partial IReadOnlyList<GpioPinItem> PagePins { get; set; } = [];

    [ObservableProperty]
    public partial int FirstPin { get; set; }

    [ObservableProperty]
    public partial int LastPin { get; set; }

    [ObservableProperty]
    public partial int PageNumber { get; set; }

    [ObservableProperty]
    public partial int PageCount { get; set; }

    public GpioViewModel(IRaspberryService raspberryService)
    {
        this.raspberryService = raspberryService;
        Pins = Header.Select(static (x, i) => new GpioPinItem(i + 1, x.Name, x.SocPin)).ToArray();
        ShowPage(0);

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

    protected override ValueTask OnNavigationForwardAsync()
    {
        ShowPage(page + 1);
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnNavigationBackAsync()
    {
        ShowPage(page - 1);
        return ValueTask.CompletedTask;
    }

    private void ShowPage(int value)
    {
        var count = (Pins.Count + PageSize - 1) / PageSize;
        page = ((value % count) + count) % count;
        var first = page * PageSize;
        PagePins = Pins.Skip(first).Take(PageSize).ToArray();
        FirstPin = first + 1;
        LastPin = first + PagePins.Count;
        PageNumber = page + 1;
        PageCount = count;
    }

    private void Refresh()
    {
        if (raspberryService.ReadGpio() is not { } states)
        {
            return;
        }

        foreach (var state in states)
        {
            if ((state.PhysicalPin < 1) || (state.PhysicalPin > Pins.Count))
            {
                continue;
            }

            var pin = Pins[state.PhysicalPin - 1];
            pin.Function = state.Function;
            pin.IsHigh = state.Level != 0;
        }
    }
}
