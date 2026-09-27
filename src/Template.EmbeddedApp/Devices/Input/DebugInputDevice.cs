namespace Template.EmbeddedApp.Devices.Input;

using Template.EmbeddedApp.State;

public sealed class DebugInputDevice : IInputDevice, IDisposable
{
    public event EventHandler<EventArgs<InputSignal>>? Handle;

    private readonly Lock sync = new();

    private readonly Dictionary<InputKey, InputGestureDetector> detectors = [];

    private readonly HashSet<InputKey> pressed = [];

    private readonly DeviceStatus status;

    public bool IsConnected => true;

    public DebugInputDevice(TimeProvider timeProvider, InputOption option, DeviceState deviceState)
    {
        var profile = option.Profiles[option.Profile];
        var buttons = option.Type == InputDeviceType.Gpio
            ? profile.Gpio
            : profile.Pad.Concat(profile.PadAxes.SelectMany(AxisButtons));
        foreach (var button in buttons.Where(static x => x.Key != InputKey.Unknown).DistinctBy(static x => x.Key))
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

    public bool IsPressed(InputKey key)
    {
        lock (sync)
        {
            return pressed.Contains(key);
        }
    }

    public void Press(InputKey key)
    {
        lock (sync)
        {
            pressed.Add(key);
        }

        status.ReportEvent();
        if (detectors.TryGetValue(key, out var detector))
        {
            detector.Down();
        }
    }

    public void Release(InputKey key)
    {
        lock (sync)
        {
            pressed.Remove(key);
        }

        status.ReportEvent();
        if (detectors.TryGetValue(key, out var detector))
        {
            detector.Up();
        }
    }

    public void Hold(InputKey key, bool down)
    {
        if (down)
        {
            Press(key);
        }
        else
        {
            Release(key);
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

    private static IEnumerable<InputButtonOption> AxisButtons(PadAxisOption axis)
    {
        yield return new InputButtonOption { Key = axis.Negative };
        yield return new InputButtonOption { Key = axis.Positive };
    }

    private void Raise(InputKey key, InputAction action)
    {
        Handle?.Invoke(this, new EventArgs<InputSignal>(new InputSignal(key, action)));
    }
}
