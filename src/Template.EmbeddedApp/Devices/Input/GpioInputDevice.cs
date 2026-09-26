namespace Template.EmbeddedApp.Devices.Input;

using System.Device.Gpio;

using Template.EmbeddedApp.State;

public sealed class GpioInputDevice : IInputDevice, IDisposable
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

    public event EventHandler<EventArgs<InputSignal>>? Handle;

    private readonly TimeProvider timeProvider;

    private readonly Dictionary<int, PinButton> buttons = [];

    private readonly DeviceStatus status;

    private readonly CancellationTokenSource cts = new();

    private readonly Task loopTask;

    private GpioController? controller;

    public GpioInputDevice(TimeProvider timeProvider, InputOption option, DeviceState deviceState)
    {
        this.timeProvider = timeProvider;

        foreach (var button in option.Profiles[option.Profile].Gpio)
        {
            var key = button.Key;
            buttons.Add(button.Pin, new PinButton(button, new InputGestureDetector(timeProvider, button, x => Raise(key, x))));
        }

        status = deviceState.Register("GPIO", true);
        var token = cts.Token;
        loopTask = Task.Run(() => LoopAsync(token), token);
        status.ReportStarted();
    }

    public void Dispose()
    {
        cts.Cancel();
        try
        {
            loopTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }

        cts.Dispose();
        controller?.Dispose();

        foreach (var button in buttons.Values)
        {
            button.Detector.Dispose();
        }
    }

    private async Task LoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (TryOpen())
            {
                return;
            }

            await Task.Delay(RetryInterval, timeProvider, token).ConfigureAwait(false);
        }
    }

    private bool TryOpen()
    {
        GpioController? gpio = null;
        try
        {
            gpio = new GpioController();
            foreach (var button in buttons.Values)
            {
                gpio.OpenPin(button.Option.Pin, button.Option.ActiveLow ? PinMode.InputPullUp : PinMode.InputPullDown);
                gpio.RegisterCallbackForPinValueChangedEvent(button.Option.Pin, PinEventTypes.Falling | PinEventTypes.Rising, OnPinValueChanged);
            }

            controller = gpio;
            gpio = null;
            status.ReportConnected();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or PlatformNotSupportedException or ArgumentException)
        {
            status.ReportError(ex.Message);
            return false;
        }
        finally
        {
            gpio?.Dispose();
        }
    }

    private void OnPinValueChanged(object sender, PinValueChangedEventArgs args)
    {
        if (!buttons.TryGetValue(args.PinNumber, out var button))
        {
            return;
        }

        var timestamp = timeProvider.GetTimestamp();
        if (timeProvider.GetElapsedTime(button.LastTimestamp, timestamp).TotalMilliseconds < button.Option.DebounceMilliseconds)
        {
            return;
        }

        button.LastTimestamp = timestamp;
        status.ReportEvent();

        if ((args.ChangeType == PinEventTypes.Falling) == button.Option.ActiveLow)
        {
            button.Detector.Down();
        }
        else
        {
            button.Detector.Up();
        }
    }

    private void Raise(InputKey key, InputAction action)
    {
        Handle?.Invoke(this, new EventArgs<InputSignal>(new InputSignal(key, action)));
    }

    private sealed class PinButton
    {
        public GpioButtonOption Option { get; }

        public InputGestureDetector Detector { get; }

        public long LastTimestamp { get; set; }

        public PinButton(GpioButtonOption option, InputGestureDetector detector)
        {
            Option = option;
            Detector = detector;
        }
    }
}
