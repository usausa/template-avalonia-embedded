namespace Template.EmbeddedApp.Devices.Input;

using LinuxDotNet.GameInput;

using Template.EmbeddedApp.State;

public sealed class PadInputDevice : IInputDevice, IDisposable
{
    public event EventHandler<EventArgs<InputSignal>>? Handle;

    private readonly Dictionary<byte, InputGestureDetector> detectors = [];

    private readonly DeviceStatus status;

    private readonly GameController controller;

    public PadInputDevice(TimeProvider timeProvider, InputOption option, DeviceState deviceState)
    {
        foreach (var button in option.Profiles[option.Profile].Pad)
        {
            var key = button.Key;
            detectors.Add(button.Button, new InputGestureDetector(timeProvider, button, x => Raise(key, x)));
        }

        status = deviceState.Register("Pad", true);
        controller = new GameController(option.PadDevice);
        controller.ButtonChanged += OnButtonChanged;
        controller.ConnectionChanged += OnConnectionChanged;
        controller.Start();
        status.ReportStarted();
    }

    public void Dispose()
    {
        controller.ButtonChanged -= OnButtonChanged;
        controller.ConnectionChanged -= OnConnectionChanged;
        controller.Dispose();

        foreach (var detector in detectors.Values)
        {
            detector.Dispose();
        }
    }

    private void OnButtonChanged(byte button, bool pressed)
    {
        status.ReportEvent();
        if (!detectors.TryGetValue(button, out var detector))
        {
            return;
        }

        if (pressed)
        {
            detector.Down();
        }
        else
        {
            detector.Up();
        }
    }

    private void OnConnectionChanged(bool connected)
    {
        if (connected)
        {
            status.ReportConnected();
            return;
        }

        status.ReportDisconnected();
        foreach (var detector in detectors.Values)
        {
            detector.Reset();
        }
    }

    private void Raise(InputKey key, InputAction action)
    {
        Handle?.Invoke(this, new EventArgs<InputSignal>(new InputSignal(key, action)));
    }
}
