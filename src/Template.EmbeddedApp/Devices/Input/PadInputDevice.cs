namespace Template.EmbeddedApp.Devices.Input;

using LinuxDotNet.GameInput;

using Template.EmbeddedApp.State;

public sealed class PadInputDevice : IInputDevice, IDisposable
{
    public event EventHandler<EventArgs<InputSignal>>? Handle;

    private readonly Dictionary<byte, InputGestureDetector> detectors = [];

    private readonly List<PadButtonOption> buttons = [];

    private readonly List<PadAxis> axes = [];

    private readonly DeviceStatus status;

    private readonly GameController controller;

    private volatile bool connected;

    public bool IsConnected => connected;

    public PadInputDevice(TimeProvider timeProvider, InputOption option, DeviceState deviceState)
    {
        var profile = option.Profiles[option.Profile];
        foreach (var button in profile.Pad)
        {
            var key = button.Key;
            detectors.Add(button.Button, new InputGestureDetector(timeProvider, button, x => Raise(key, x)));
            buttons.Add(button);
        }

        foreach (var axis in profile.PadAxes)
        {
            axes.Add(new PadAxis(axis, CreateDetector(timeProvider, axis.Negative), CreateDetector(timeProvider, axis.Positive)));
        }

        status = deviceState.Register("Pad", true);
        controller = new GameController(option.PadDevice);
        controller.ButtonChanged += OnButtonChanged;
        controller.AxisChanged += OnAxisChanged;
        controller.ConnectionChanged += OnConnectionChanged;
        controller.Start();
        status.ReportStarted();
    }

    public void Dispose()
    {
        connected = false;
        controller.ButtonChanged -= OnButtonChanged;
        controller.AxisChanged -= OnAxisChanged;
        controller.ConnectionChanged -= OnConnectionChanged;
        controller.Dispose();

        foreach (var detector in detectors.Values)
        {
            detector.Dispose();
        }

        foreach (var axis in axes)
        {
            axis.Dispose();
        }
    }

    public bool IsPressed(InputKey key)
    {
        if (!connected)
        {
            return false;
        }

        foreach (var button in buttons)
        {
            if ((button.Key == key) && controller.GetButtonPressed(button.Button))
            {
                return true;
            }
        }

        foreach (var axis in axes)
        {
            if (axis.IsPressed(key, controller.GetAxisValue(axis.Option.Axis)))
            {
                return true;
            }
        }

        return false;
    }

    private InputGestureDetector? CreateDetector(TimeProvider timeProvider, InputKey key) =>
        key == InputKey.Unknown ? null : new InputGestureDetector(timeProvider, new InputButtonOption { Key = key }, x => Raise(key, x));

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

    private void OnAxisChanged(byte axis, short value)
    {
        status.ReportEvent();
        foreach (var entry in axes)
        {
            if (entry.Option.Axis == axis)
            {
                entry.Update(value);
            }
        }
    }

    private void OnConnectionChanged(bool value)
    {
        connected = value;
        if (value)
        {
            status.ReportConnected();
            return;
        }

        status.ReportDisconnected();
        foreach (var detector in detectors.Values)
        {
            detector.Reset();
        }

        foreach (var axis in axes)
        {
            axis.Reset();
        }
    }

    private void Raise(InputKey key, InputAction action)
    {
        Handle?.Invoke(this, new EventArgs<InputSignal>(new InputSignal(key, action)));
    }

    private sealed class PadAxis : IDisposable
    {
        private readonly InputGestureDetector? negative;

        private readonly InputGestureDetector? positive;

        private bool negativePressed;

        private bool positivePressed;

        public PadAxisOption Option { get; }

        public PadAxis(PadAxisOption option, InputGestureDetector? negative, InputGestureDetector? positive)
        {
            Option = option;
            this.negative = negative;
            this.positive = positive;
        }

        public void Dispose()
        {
            negative?.Dispose();
            positive?.Dispose();
        }

        public bool IsPressed(InputKey key, short value) =>
            ((Option.Negative == key) && (value <= -Option.Threshold)) || ((Option.Positive == key) && (value >= Option.Threshold));

        public void Update(short value)
        {
            negativePressed = Change(negative, negativePressed, value <= -Option.Threshold);
            positivePressed = Change(positive, positivePressed, value >= Option.Threshold);
        }

        public void Reset()
        {
            negativePressed = false;
            positivePressed = false;
            negative?.Reset();
            positive?.Reset();
        }

        private static bool Change(InputGestureDetector? detector, bool before, bool after)
        {
            if (before != after)
            {
                if (after)
                {
                    detector?.Down();
                }
                else
                {
                    detector?.Up();
                }
            }

            return after;
        }
    }
}
