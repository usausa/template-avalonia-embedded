namespace Template.EmbeddedApp.Devices.Input;

using LinuxDotNet.GameInput;

using Template.EmbeddedApp.State;

public sealed class PadInputDevice : IInputDevice, IDisposable
{
    public event EventHandler<EventArgs<InputSignal>>? Handle;

    private readonly Dictionary<byte, InputGestureDetector> detectors = [];

    private readonly List<PadButtonOption> buttons = [];

    private readonly List<PadAxis> axes = [];

    private readonly GameController controller;

    private volatile bool connected;

    public DeviceStatus Status { get; }

    public bool IsConnected => connected;

    public IReadOnlyList<InputKeyBinding> Bindings { get; }

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

        Bindings = CreateBindings(profile);
        Status = deviceState.Register("Pad", true);
        controller = new GameController(option.PadDevice);
        controller.ButtonChanged += OnButtonChanged;
        controller.AxisChanged += OnAxisChanged;
        controller.ConnectionChanged += OnConnectionChanged;
        controller.Start();
        Status.ReportStarted();
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
        Status.ReportEvent();
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
        Status.ReportEvent();
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
            Status.ReportConnected();
            return;
        }

        Status.ReportDisconnected();
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

    private static InputKeyBinding[] CreateBindings(InputProfileOption profile) =>
        profile.Pad
            .Select(static x => (x.Key, Source: String.Create(CultureInfo.InvariantCulture, $"button {x.Button}")))
            .Concat(profile.PadAxes.SelectMany(static x => new[]
            {
                (Key: x.Negative, Source: String.Create(CultureInfo.InvariantCulture, $"axis {x.Axis} -")),
                (Key: x.Positive, Source: String.Create(CultureInfo.InvariantCulture, $"axis {x.Axis} +"))
            }))
            .Where(static x => x.Key != InputKey.Unknown)
            .GroupBy(static x => x.Key)
            .OrderBy(static x => x.Key)
            .Select(static x => new InputKeyBinding(x.Key, String.Join(", ", x.Select(static y => y.Source))))
            .ToArray();

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
