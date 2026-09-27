namespace Template.EmbeddedApp.Devices.BuildHat;

using Template.EmbeddedApp.State;

public sealed class BuildHatDriveController : IDriveController, IDisposable
{
    private const int PortCount = 4;

    private const string ActivePrefix = "connected to active ID ";

    private const string PassivePrefix = "connected to passive ID ";

    private const double MinimumRampSeconds = 0.05;

    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan VoltageInterval = TimeSpan.FromSeconds(5);

    private readonly Lock sync = new();

    private readonly ILogger<BuildHatDriveController> log;

    private readonly TimeProvider timeProvider;

    private readonly BuildHatOption option;

    private readonly DeviceStatus status;

    private readonly PortState[] ports = [new(), new(), new(), new()];

    private readonly CancellationTokenSource cts = new();

    private readonly Task? loopTask;

    private BuildHatConnection? connection;

    private string firmware = string.Empty;

    private double? voltage;

    private bool powerFault;

    private int steeringAngle;

    public BuildHatDriveController(ILogger<BuildHatDriveController> log, TimeProvider timeProvider, BuildHatOption option, DeviceState deviceState)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        this.option = option;
        status = deviceState.Register("Build HAT", !String.IsNullOrEmpty(option.Device));
        if (!status.IsEnabled)
        {
            return;
        }

        var token = cts.Token;
        loopTask = Task.Factory.StartNew(() => Run(token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        status.ReportStarted();
    }

    public void Dispose()
    {
        cts.Cancel();
        if (loopTask is not null)
        {
            try
            {
                loopTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
        }

        cts.Dispose();
    }

    public BuildHatStatus GetStatus()
    {
        lock (sync)
        {
            var condition = !status.IsEnabled ? DeviceCondition.Disabled : connection is null ? DeviceCondition.Waiting : DeviceCondition.Connected;
            return new BuildHatStatus(condition, firmware, voltage, powerFault, CreateDriveStatus(), CreateSteeringStatus());
        }
    }

    public void Drive(int speed)
    {
        lock (sync)
        {
            var index = (int)option.Drive.Port;
            var state = ports[index];
            if ((connection is null) || (state.Type is not { IsMotor: true }))
            {
                return;
            }

            var value = option.Drive.Reverse ? -speed : speed;
            if (value == 0)
            {
                Coast(index);
            }
            else if (state.Mode != MotorMode.Speed)
            {
                Send(String.Create(CultureInfo.InvariantCulture, $"port {index} ; pid {index} 0 0 s1 1 0 0.003 0.01 0 100 ; set {value}"));
                state.Mode = MotorMode.Speed;
                state.Command = value;
            }
            else if (state.Command != value)
            {
                Send(String.Create(CultureInfo.InvariantCulture, $"port {index} ; set {value}"));
                state.Command = value;
            }
        }
    }

    public void Steer(int angle)
    {
        lock (sync)
        {
            steeringAngle = Math.Clamp(angle, -option.Steering.MaxAngle, option.Steering.MaxAngle);
            ApplySteering();
        }
    }

    public void Halt()
    {
        lock (sync)
        {
            Coast((int)option.Drive.Port);
            steeringAngle = 0;
            ApplySteering();
        }
    }

    private void Run(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (TryOpen(token) is not { } opened)
            {
                token.WaitHandle.WaitOne(RetryInterval);
                continue;
            }

            try
            {
                Receive(opened, token);
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException or TimeoutException)
            {
                status.ReportError(ex.Message);
            }
            finally
            {
                Close();
            }
        }
    }

    private BuildHatConnection? TryOpen(CancellationToken token)
    {
        try
        {
            var opened = BuildHatConnection.Open(option.Device, token);
            lock (sync)
            {
                connection = opened;
                firmware = opened.Firmware;
                voltage = null;
                powerFault = false;
            }

            log.InfoBuildHatOpened(opened.Firmware, opened.FirmwareLoaded);
            status.ReportConnected();
            return opened;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException or ArgumentException)
        {
            status.ReportError(ex.Message);
            return null;
        }
    }

    private void Receive(BuildHatConnection opened, CancellationToken token)
    {
        lock (sync)
        {
            Send("list");
            Send("vin");
        }

        var voltageTimestamp = timeProvider.GetTimestamp();
        while (!token.IsCancellationRequested)
        {
            if (opened.ReadLine() is { } line)
            {
                Process(line);
            }

            if (timeProvider.GetElapsedTime(voltageTimestamp) >= VoltageInterval)
            {
                lock (sync)
                {
                    Send("vin");
                }

                voltageTimestamp = timeProvider.GetTimestamp();
            }
        }
    }

    private void Close()
    {
        lock (sync)
        {
            if (connection is null)
            {
                return;
            }

            try
            {
                for (var i = 0; i < PortCount; i++)
                {
                    connection.Send(String.Create(CultureInfo.InvariantCulture, $"port {i} ; select ; pwm ; coast ; off"));
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException)
            {
                status.ReportError(ex.Message);
            }

            connection.Dispose();
            connection = null;
            foreach (var port in ports)
            {
                port.Reset();
            }
        }

        status.ReportDisconnected();
    }

    private void Process(string line)
    {
        if ((line.Length > 3) && (line[0] == 'P') && Char.IsAsciiDigit(line[1]) && (line[1] - '0' < PortCount))
        {
            var index = line[1] - '0';
            if (line[2] == ':')
            {
                ProcessPort(index, line[3..].Trim());
            }
            else if (line.AsSpan(2).StartsWith("C0:", StringComparison.Ordinal))
            {
                ProcessData(index, line[5..]);
            }

            return;
        }

        if (line.Contains("power fault", StringComparison.OrdinalIgnoreCase))
        {
            ProcessPowerFault();
            return;
        }

        if (line.EndsWith(" V", StringComparison.Ordinal) &&
            Double.TryParse(line.AsSpan(0, line.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            lock (sync)
            {
                voltage = value;
            }
        }
    }

    private void ProcessPort(int index, string message)
    {
        if (message.StartsWith(ActivePrefix, StringComparison.Ordinal))
        {
            Connect(index, message[ActivePrefix.Length..], true);
        }
        else if (message.StartsWith(PassivePrefix, StringComparison.Ordinal))
        {
            Connect(index, message[PassivePrefix.Length..], false);
        }
        else if (message.StartsWith("disconnected", StringComparison.Ordinal) ||
                 message.StartsWith("no device detected", StringComparison.Ordinal) ||
                 message.Contains("disconnecting", StringComparison.Ordinal))
        {
            Disconnect(index);
        }
    }

    private void Connect(int index, string id, bool active)
    {
        if (!Int32.TryParse(id.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            return;
        }

        var type = BuildHatDeviceType.Find(value, active);
        lock (sync)
        {
            var state = ports[index];
            state.Reset();
            state.Type = type;
            if (type.IsMotor)
            {
                Send(String.Create(CultureInfo.InvariantCulture, $"port {index} ; combi 0 1 0 2 0{(type.HasAbsolutePosition ? " 3 0" : string.Empty)} ; select 0"));
            }
        }

        log.InfoBuildHatPortConnected((BuildHatPort)index, type.Name);
    }

    private void Disconnect(int index)
    {
        lock (sync)
        {
            if (ports[index].Type is null)
            {
                return;
            }

            ports[index].Reset();
        }

        log.InfoBuildHatPortDisconnected((BuildHatPort)index);
    }

    private void ProcessData(int index, string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if ((parts.Length < 2) ||
            !Int32.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var speed) ||
            !Int32.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var position))
        {
            return;
        }

        int? absolute = (parts.Length > 2) && Int32.TryParse(parts[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) ? value : null;
        lock (sync)
        {
            var state = ports[index];
            if (state.Type is not { IsMotor: true } type)
            {
                return;
            }

            state.Speed = speed;
            state.Position = position;
            state.Absolute = type.HasAbsolutePosition ? absolute : null;
            if ((index == (int)option.Steering.Port) && (state.Center is null) && (state.Absolute is { } current))
            {
                state.Center = position + Wrap(option.Steering.Center - current);
                ApplySteering();
            }
        }

        status.ReportEvent();
    }

    private void ProcessPowerFault()
    {
        lock (sync)
        {
            powerFault = true;
            Coast((int)option.Drive.Port);
        }

        log.WarnBuildHatPowerFault();
        status.ReportError("Power fault.");
    }

    private void Coast(int index)
    {
        var state = ports[index];
        if ((connection is null) || (state.Mode == MotorMode.None))
        {
            return;
        }

        Send(String.Create(CultureInfo.InvariantCulture, $"port {index} ; pwm ; coast"));
        state.Mode = MotorMode.None;
        state.Command = 0;
    }

    private void ApplySteering()
    {
        var index = (int)option.Steering.Port;
        var state = ports[index];
        if ((connection is null) || (state.Center is not { } center))
        {
            return;
        }

        var target = center + (option.Steering.Reverse ? -steeringAngle : steeringAngle);
        if ((state.Mode == MotorMode.Position) && (state.Command == target))
        {
            return;
        }

        var from = state.Position / 360d;
        var to = target / 360d;
        var duration = Math.Max(MinimumRampSeconds, Math.Abs(to - from) / (option.Steering.Speed * 0.05));
        Send(String.Create(CultureInfo.InvariantCulture, $"port {index} ; pid {index} 0 1 s4 0.0027777778 0 5 0 .1 3 ; set ramp {from:F4} {to:F4} {duration:F3} 0"));
        state.Mode = MotorMode.Position;
        state.Command = target;
    }

    private void Send(string command)
    {
        try
        {
            connection?.Send(command);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException)
        {
            status.ReportError(ex.Message);
        }
    }

    private MotorStatus CreateDriveStatus()
    {
        var state = ports[(int)option.Drive.Port];
        var sign = option.Drive.Reverse ? -1 : 1;
        return new MotorStatus(option.Drive.Port, GetLink(state, false), state.Type?.Name ?? string.Empty, state.Speed * sign, state.Position * sign, state.Absolute, null);
    }

    private MotorStatus CreateSteeringStatus()
    {
        var state = ports[(int)option.Steering.Port];
        var sign = option.Steering.Reverse ? -1 : 1;
        int? angle = state.Absolute is { } absolute ? Wrap(absolute - option.Steering.Center) * sign : null;
        return new MotorStatus(option.Steering.Port, GetLink(state, true), state.Type?.Name ?? string.Empty, state.Speed * sign, state.Position * sign, state.Absolute, angle);
    }

    private MotorLink GetLink(PortState state, bool absolute)
    {
        if (!status.IsEnabled)
        {
            return MotorLink.Disabled;
        }

        if ((connection is null) || (state.Type is null))
        {
            return MotorLink.Waiting;
        }

        return state.Type.IsMotor && (!absolute || state.Type.HasAbsolutePosition) ? MotorLink.Connected : MotorLink.Unsupported;
    }

    private static int Wrap(int degree) => ((((degree + 180) % 360) + 360) % 360) - 180;

    private enum MotorMode
    {
        None,
        Speed,
        Position
    }

    private sealed class PortState
    {
        public BuildHatDeviceType? Type { get; set; }

        public int Speed { get; set; }

        public int Position { get; set; }

        public int? Absolute { get; set; }

        public int? Center { get; set; }

        public MotorMode Mode { get; set; }

        public int Command { get; set; }

        public void Reset()
        {
            Type = null;
            Speed = 0;
            Position = 0;
            Absolute = null;
            Center = null;
            Mode = MotorMode.None;
            Command = 0;
        }
    }
}
