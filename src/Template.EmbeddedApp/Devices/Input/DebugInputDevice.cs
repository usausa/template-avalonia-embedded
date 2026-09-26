namespace Template.EmbeddedApp.Devices.Input;

using Template.EmbeddedApp.State;

public sealed class DebugInputDevice : IInputDevice, IDisposable
{
    public event EventHandler<EventArgs<InputSignal>>? Handle;

    private readonly Dictionary<InputKey, InputGestureDetector> detectors = [];

    private readonly DeviceStatus status;

    public DebugInputDevice(TimeProvider timeProvider, InputOption option, DeviceState deviceState)
    {
        var profile = option.Profiles[option.Profile];
        IEnumerable<InputButtonOption> buttons = option.Type == InputDeviceType.Gpio ? profile.Gpio : profile.Pad;
        foreach (var button in buttons.DistinctBy(static x => x.Key))
        {
            var key = button.Key;
            detectors.Add(key, new InputGestureDetector(timeProvider, button, x => Raise(key, x)));
        }

        status = deviceState.Register("Debug", true);
        status.ReportStarted();
        status.ReportConnected();
    }

    public void Dispose()
    {
        foreach (var detector in detectors.Values)
        {
            detector.Dispose();
        }
    }

    public void Press(InputKey key)
    {
        status.ReportEvent();
        if (detectors.TryGetValue(key, out var detector))
        {
            detector.Down();
        }
    }

    public void Release(InputKey key)
    {
        status.ReportEvent();
        if (detectors.TryGetValue(key, out var detector))
        {
            detector.Up();
        }
    }

    public void Trigger(InputKey key)
    {
        Press(key);
        Release(key);
    }

    public void LongPress(InputKey key)
    {
        status.ReportEvent();
        Raise(key, InputAction.LongPress);
    }

    private void Raise(InputKey key, InputAction action)
    {
        Handle?.Invoke(this, new EventArgs<InputSignal>(new InputSignal(key, action)));
    }
}
