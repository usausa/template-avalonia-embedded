namespace Template.EmbeddedApp.Services;

using RaspberryDotNet.SystemInfo;

public sealed class RaspberryService : IRaspberryService, IDisposable
{
    private readonly Lock sync = new();

    private readonly Vcio vcio = new();

    private readonly GpioMap gpio = new();

    public bool IsSupported { get; } = Vcio.IsSupported();

    public bool IsGpioSupported { get; } = GpioMap.IsSupported();

    public void Dispose()
    {
        lock (sync)
        {
            vcio.Dispose();
            gpio.Dispose();
        }
    }

    public RaspberrySnapshot? Read()
    {
        if (!IsSupported)
        {
            return null;
        }

        lock (sync)
        {
            if (!vcio.IsOpen && !vcio.Open())
            {
                return null;
            }

            return new RaspberrySnapshot(
                vcio.ReadTemperature(),
                ReadClock(ClockType.Arm),
                ReadClock(ClockType.Core),
                vcio.ReadVoltage(VoltageType.Core),
                vcio.ReadThrottled());
        }
    }

    public IReadOnlyList<GpioHeaderPinState>? ReadGpio()
    {
        if (!IsGpioSupported)
        {
            return null;
        }

        lock (sync)
        {
            if (!gpio.IsOpen && !gpio.Open())
            {
                return null;
            }

            return gpio.ReadHeaderGpioPins();
        }
    }

    private double ReadClock(ClockType type)
    {
        var value = vcio.ReadFrequency(type);
        return Double.IsNaN(value) ? vcio.ReadFrequency(type, false) : value;
    }
}
