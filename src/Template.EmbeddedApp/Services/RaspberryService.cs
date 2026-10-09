namespace Template.EmbeddedApp.Services;

using RaspberryDotNet.SystemInfo;

public sealed record RaspberrySnapshot(double Temperature, double ArmClock, double CoreClock, double CoreVoltage, ThrottledFlags Throttled);

public sealed class RaspberryService : IDisposable
{
    private readonly Lock sync = new();

    private readonly VcioMonitor? vcio = OperatingSystem.IsLinux() ? PlatformProvider.GetVcioMonitor() : null;

    private readonly GpioMonitor? gpio = OperatingSystem.IsLinux() ? PlatformProvider.GetGpioMonitor() : null;

    private bool disposed;

    public bool IsSupported => vcio?.Supported ?? false;

    public bool IsGpioSupported => gpio?.Supported ?? false;

    public void Dispose()
    {
        lock (sync)
        {
            disposed = true;
            vcio?.Dispose();
            gpio?.Dispose();
        }
    }

    public RaspberrySnapshot? Read()
    {
        if (vcio is not { Supported: true })
        {
            return null;
        }

        lock (sync)
        {
            if (disposed)
            {
                return null;
            }

            vcio.Update();

            return new RaspberrySnapshot(
                vcio.Temperature,
                FindClock(vcio, ClockType.Arm),
                FindClock(vcio, ClockType.Core),
                FindVoltage(vcio, VoltageType.Core),
                vcio.Throttled);
        }
    }

    public IReadOnlyList<GpioPin>? ReadGpio()
    {
        if (gpio is not { Supported: true })
        {
            return null;
        }

        lock (sync)
        {
            if (disposed)
            {
                return null;
            }

            gpio.Update();

            return gpio.Pins;
        }
    }

    private static double FindClock(VcioMonitor monitor, ClockType type) =>
        monitor.Clocks.FirstOrDefault(x => x.Type == type)?.Frequency ?? Double.NaN;

    private static double FindVoltage(VcioMonitor monitor, VoltageType type) =>
        monitor.Voltages.FirstOrDefault(x => x.Type == type)?.Voltage ?? Double.NaN;
}
