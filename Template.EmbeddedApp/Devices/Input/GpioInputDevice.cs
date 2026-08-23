namespace Template.EmbeddedApp.Devices.Input;

using System.Device.Gpio;

using Template.EmbeddedApp.Settings;

public sealed class GpioInputDevice : IInputDevice, IDisposable
{
    public event EventHandler<EventArgs<InputKey>>? Handle;

    private readonly GpioController controller = new();

    private readonly TimeProvider timeProvider;

    private readonly int debounceMilliseconds;

    private readonly int[] pins;

    private readonly long[] lastTimestamps;

    public GpioInputDevice(GpioInputSetting setting, TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
        debounceMilliseconds = setting.DebounceMilliseconds;
        pins = [.. setting.Pins];
        lastTimestamps = new long[pins.Length];

        for (var i = 0; i < pins.Length; i++)
        {
            var index = i;
            controller.OpenPin(pins[i], PinMode.InputPullUp);
            controller.RegisterCallbackForPinValueChangedEvent(pins[i], PinEventTypes.Falling, (_, _) => HandlePinEvent(index));
        }
    }

    public void Dispose()
    {
        controller.Dispose();
    }

    private void HandlePinEvent(int index)
    {
        var timestamp = timeProvider.GetTimestamp();
        if (timeProvider.GetElapsedTime(lastTimestamps[index], timestamp).TotalMilliseconds < debounceMilliseconds)
        {
            return;
        }

        lastTimestamps[index] = timestamp;

        var key = index switch
        {
            0 => InputKey.Button1,
            1 => InputKey.Button2,
            2 => InputKey.Button3,
            3 => InputKey.Button4,
            _ => InputKey.Unknown
        };

        Handle?.Invoke(this, new EventArgs<InputKey>(key));
    }
}
