namespace Template.EmbeddedApp.Devices.BuildHat;

using RaspberryDotNet.BuildHat;

using Template.EmbeddedApp.State;

public sealed class BuildHatDriveController : IDriveController, IDisposable
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan MonitorInterval = TimeSpan.FromMilliseconds(100);

    private readonly Lock sync = new();

    private readonly ILogger<BuildHatDriveController> log;

    private readonly BuildHatOption option;

    private readonly DeviceStatus status;

    private readonly BuildHatController controller;

    private readonly BuildHatMotor driveMotor;

    private readonly BuildHatMotor steeringMotor;

    private readonly CancellationTokenSource cts = new();

    private readonly Task? loopTask;

    private bool connected;

    private int? steeringCenter;

    private int steeringAngle;

    private (int Drive, int Steering) lastPosition;

    public BuildHatDriveController(ILogger<BuildHatDriveController> log, BuildHatOption option, DeviceState deviceState)
    {
        this.log = log;
        this.option = option;
        status = deviceState.Register("Build HAT", !String.IsNullOrEmpty(option.Device));
        controller = new BuildHatController(new BuildHatOptions { Device = option.Device });
        driveMotor = controller.GetMotor((int)option.Drive.Port);
        steeringMotor = controller.GetMotor((int)option.Steering.Port);
        controller.PortChanged += OnPortChanged;
        controller.FaultDetected += OnFaultDetected;
        controller.ConnectionLost += OnConnectionLost;
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

        controller.PortChanged -= OnPortChanged;
        controller.FaultDetected -= OnFaultDetected;
        controller.ConnectionLost -= OnConnectionLost;
        controller.Dispose();
        cts.Dispose();
    }

    public DriveStatus GetStatus()
    {
        var hat = controller.Status;
        lock (sync)
        {
            var condition = !status.IsEnabled ? DeviceCondition.Disabled : connected ? DeviceCondition.Connected : DeviceCondition.Waiting;
            return new DriveStatus(condition, hat.Firmware, hat.Voltage, hat.HasPowerFault, CreateDriveStatus(), CreateSteeringStatus());
        }
    }

    public void Drive(int speed)
    {
        lock (sync)
        {
            if (!connected || (driveMotor.State.DeviceType is not { Support: BuildHatDeviceSupport.Motor }))
            {
                return;
            }

            var value = option.Drive.Reverse ? -speed : speed;
            try
            {
                if (value == 0)
                {
                    driveMotor.Coast();
                }
                else
                {
                    driveMotor.SetSpeed(value);
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                status.ReportError(ex.Message);
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
            CoastDrive();
            steeringAngle = 0;
            ApplySteering();
        }
    }

    private void Run(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (!TryOpen(token))
            {
                token.WaitHandle.WaitOne(RetryInterval);
                continue;
            }

            try
            {
                while (!token.WaitHandle.WaitOne(MonitorInterval) && controller.IsOpen)
                {
                    Monitor();
                }
            }
            finally
            {
                Close();
            }
        }
    }

    private bool TryOpen(CancellationToken token)
    {
        try
        {
            controller.Open(token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException or ArgumentException)
        {
            status.ReportError(ex.Message);
            return false;
        }

        lock (sync)
        {
            connected = true;
            steeringCenter = null;
        }

        var hat = controller.Status;
        log.InfoBuildHatOpened(hat.Firmware, hat.FirmwareLoaded);
        status.ReportConnected();
        return true;
    }

    private void Close()
    {
        lock (sync)
        {
            connected = false;
            steeringCenter = null;
        }

        controller.Close();
        status.ReportDisconnected();
    }

    private void Monitor()
    {
        bool moved;
        lock (sync)
        {
            if ((steeringCenter is null) &&
                (steeringMotor.State is { DeviceType: { Support: BuildHatDeviceSupport.Motor, HasAbsolutePosition: true }, AbsolutePosition: { } absolute } steering))
            {
                steeringCenter = steering.Position + Wrap(option.Steering.Center - absolute);
                ApplySteering();
            }

            var position = (driveMotor.State.Position, steeringMotor.State.Position);
            moved = position != lastPosition;
            lastPosition = position;
        }

        if (moved)
        {
            status.ReportEvent();
        }
    }

    private void OnPortChanged(object? sender, BuildHatPortEventArgs e)
    {
        if (e.Port.Index == (int)option.Steering.Port)
        {
            lock (sync)
            {
                steeringCenter = null;
            }
        }

        if (e.DeviceType is { } type)
        {
            log.InfoBuildHatPortConnected(e.Port.Name, type.Name, type.Support);
        }
        else
        {
            log.InfoBuildHatPortDisconnected(e.Port.Name);
        }

        status.ReportEvent();
    }

    private void OnFaultDetected(object? sender, BuildHatFaultEventArgs e)
    {
        lock (sync)
        {
            CoastDrive();
        }

        log.WarnBuildHatPowerFault(e.Fault);
        status.ReportError(e.Fault == BuildHatFault.MotorPower ? "Motor power fault." : "Port power fault.");
    }

    private void OnConnectionLost(object? sender, ErrorEventArgs e)
    {
        status.ReportError(e.GetException().Message);
    }

    private void CoastDrive()
    {
        if (!connected)
        {
            return;
        }

        try
        {
            driveMotor.Coast();
        }
        catch (IOException ex)
        {
            status.ReportError(ex.Message);
        }
    }

    private void ApplySteering()
    {
        if (!connected || (steeringCenter is not { } center))
        {
            return;
        }

        var target = center + (option.Steering.Reverse ? -steeringAngle : steeringAngle);
        try
        {
            steeringMotor.MoveTo(target, option.Steering.Speed);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            status.ReportError(ex.Message);
        }
    }

    private MotorStatus CreateDriveStatus()
    {
        var state = driveMotor.State;
        var sign = option.Drive.Reverse ? -1 : 1;
        return new MotorStatus(option.Drive.Port, GetLink(state, false), state.DeviceType?.Name ?? string.Empty, state.Speed * sign, state.Position * sign, state.AbsolutePosition, null);
    }

    private MotorStatus CreateSteeringStatus()
    {
        var state = steeringMotor.State;
        var sign = option.Steering.Reverse ? -1 : 1;
        int? angle = state.AbsolutePosition is { } absolute ? Wrap(absolute - option.Steering.Center) * sign : null;
        return new MotorStatus(option.Steering.Port, GetLink(state, true), state.DeviceType?.Name ?? string.Empty, state.Speed * sign, state.Position * sign, state.AbsolutePosition, angle);
    }

    private MotorLink GetLink(BuildHatPortState state, bool absolute)
    {
        if (!status.IsEnabled)
        {
            return MotorLink.Disabled;
        }

        if (!connected || (state.DeviceType is not { } type))
        {
            return MotorLink.Waiting;
        }

        return (type.Support == BuildHatDeviceSupport.Motor) && (!absolute || type.HasAbsolutePosition) ? MotorLink.Connected : MotorLink.Unsupported;
    }

    private static int Wrap(int degree) => ((((degree + 180) % 360) + 360) % 360) - 180;
}
