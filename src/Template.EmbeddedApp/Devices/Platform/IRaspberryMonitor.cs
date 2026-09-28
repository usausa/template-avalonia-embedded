namespace Template.EmbeddedApp.Devices.Platform;

using RaspberryDotNet.SystemInfo;

public sealed record RaspberrySnapshot(double Temperature, double ArmClock, double CoreClock, double CoreVoltage, ThrottledFlags Throttled);

public interface IRaspberryMonitor
{
    bool IsSupported { get; }

    bool IsGpioSupported { get; }

    RaspberrySnapshot? Read();

    IReadOnlyList<GpioHeaderPinState>? ReadGpio();
}
