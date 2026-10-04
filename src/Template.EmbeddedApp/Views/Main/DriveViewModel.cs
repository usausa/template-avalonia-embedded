namespace Template.EmbeddedApp.Views.Main;

using Avalonia.Threading;

using Template.EmbeddedApp.Devices.BuildHat;
using Template.EmbeddedApp.Devices.Input;
using Template.EmbeddedApp.State;

public enum SteeringDirection
{
    Center,
    Left,
    Right
}

public sealed partial class DriveViewModel : AppViewModelBase
{
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(1000d / 60);

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(100);

    private readonly TimeProvider timeProvider;

    private readonly BuildHatOption option;

    private readonly IInputDevice input;

    private readonly IDriveController driveController;

    private readonly DispatcherTimer timer;

    private readonly DriveModel model = new();

    private CancellationTokenSource? cts;

    private Task? loopTask;

    private volatile DriveFrame frame = DriveFrame.Idle;

    public MotorPort DrivePort { get; }

    public MotorPort SteeringPort { get; }

    public int MinAngle { get; }

    public int MaxAngle { get; }

    [ObservableProperty]
    public partial DeviceCondition HatCondition { get; set; }

    [ObservableProperty]
    public partial bool IsHatConnected { get; set; }

    [ObservableProperty]
    public partial bool IsPowerFault { get; set; }

    [ObservableProperty]
    public partial double? HatVoltage { get; set; }

    [ObservableProperty]
    public partial string? FirmwareDate { get; set; }

    [ObservableProperty]
    public partial MotorStatus? DriveMotor { get; set; }

    [ObservableProperty]
    public partial bool IsDriveConnected { get; set; }

    [ObservableProperty]
    public partial bool IsDriveUnsupported { get; set; }

    [ObservableProperty]
    public partial bool IsForward { get; set; }

    [ObservableProperty]
    public partial int TargetSpeed { get; set; }

    [ObservableProperty]
    public partial int ActualSpeed { get; set; }

    [ObservableProperty]
    public partial int? DriveSpeed { get; set; }

    [ObservableProperty]
    public partial int? DrivePosition { get; set; }

    [ObservableProperty]
    public partial MotorStatus? SteeringMotor { get; set; }

    [ObservableProperty]
    public partial bool IsSteeringConnected { get; set; }

    [ObservableProperty]
    public partial bool IsSteeringUnsupported { get; set; }

    [ObservableProperty]
    public partial SteeringDirection SteeringDirection { get; set; }

    [ObservableProperty]
    public partial int TargetAngle { get; set; }

    [ObservableProperty]
    public partial int ActualAngle { get; set; }

    [ObservableProperty]
    public partial int? SteeringAngle { get; set; }

    [ObservableProperty]
    public partial int? SteeringAbsolute { get; set; }

    [ObservableProperty]
    public partial bool Accel { get; set; }

    [ObservableProperty]
    public partial bool Brake { get; set; }

    public DriveViewModel(TimeProvider timeProvider, BuildHatOption option, IInputDevice input, IDriveController driveController)
    {
        this.timeProvider = timeProvider;
        this.option = option;
        this.input = input;
        this.driveController = driveController;
        DrivePort = option.Drive.Port;
        SteeringPort = option.Steering.Port;
        MinAngle = -option.Steering.MaxAngle;
        MaxAngle = option.Steering.MaxAngle;

        timer = new DispatcherTimer { Interval = RefreshInterval };
        timer.Tick += (_, _) => Refresh();

        Refresh();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopLoop();
        }

        base.Dispose(disposing);
    }

    public override void OnNavigatedTo(INavigationContext context)
    {
        StartLoop();
    }

    public override void OnNavigatingFrom(INavigationContext context)
    {
        StopLoop();
    }

    private void StartLoop()
    {
        if (cts is not null)
        {
            return;
        }

        model.Reset();
        cts = new CancellationTokenSource();
        var token = cts.Token;
        loopTask = Task.Run(() => LoopAsync(token), token);
        timer.Start();
    }

    private void StopLoop()
    {
        timer.Stop();
        if (cts is null)
        {
            return;
        }

        cts.Cancel();
        try
        {
            loopTask?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }

        cts.Dispose();
        cts = null;
        loopTask = null;

        model.Reset();
        frame = DriveFrame.Idle;
        driveController.Halt();
        Refresh();
    }

    private async Task LoopAsync(CancellationToken token)
    {
        using var periodic = new PeriodicTimer(FrameInterval, timeProvider);
        while (await periodic.WaitForNextTickAsync(token).ConfigureAwait(false))
        {
            var accel = false;
            var brake = false;
            var left = false;
            var right = false;
            if (input.IsConnected)
            {
                accel = input.IsPressed(InputKey.Button1);
                brake = input.IsPressed(InputKey.Button2);
                left = input.IsPressed(InputKey.Left);
                right = input.IsPressed(InputKey.Right);
                model.Update(accel, brake, FrameInterval.TotalSeconds);
            }
            else
            {
                model.Reset();
            }

            var speed = model.ToPercent(option.Drive.MaxSpeed);
            var angle = left == right ? 0 : left ? -option.Steering.MaxAngle : option.Steering.MaxAngle;
            driveController.Drive(speed);
            driveController.Steer(angle);
            frame = new DriveFrame(speed, angle, accel, brake);
        }
    }

    private void Refresh()
    {
        var hat = driveController.GetStatus();
        var current = frame;

        HatCondition = hat.Condition;
        IsHatConnected = hat.Condition == DeviceCondition.Connected;
        IsPowerFault = hat.PowerFault;
        HatVoltage = hat.Voltage;
        FirmwareDate = GetFirmwareDate(hat.Firmware);

        DriveMotor = hat.Drive;
        IsDriveConnected = hat.Drive.Link == MotorLink.Connected;
        IsDriveUnsupported = hat.Drive.Link == MotorLink.Unsupported;
        IsForward = current.Speed > 0;
        TargetSpeed = current.Speed;
        ActualSpeed = IsDriveConnected ? Math.Abs(hat.Drive.Speed) : 0;
        DriveSpeed = IsDriveConnected ? hat.Drive.Speed : null;
        DrivePosition = IsDriveConnected ? hat.Drive.Position : null;

        SteeringMotor = hat.Steering;
        IsSteeringConnected = hat.Steering.Link == MotorLink.Connected;
        IsSteeringUnsupported = hat.Steering.Link == MotorLink.Unsupported;
        SteeringDirection = current.Angle switch
        {
            < 0 => SteeringDirection.Left,
            > 0 => SteeringDirection.Right,
            _ => SteeringDirection.Center
        };
        TargetAngle = current.Angle;
        ActualAngle = IsSteeringConnected ? (hat.Steering.Angle ?? 0) : 0;
        SteeringAngle = IsSteeringConnected ? hat.Steering.Angle : null;
        SteeringAbsolute = IsSteeringConnected ? hat.Steering.Absolute : null;

        Accel = current.Accel;
        Brake = current.Brake;
    }

    private static string? GetFirmwareDate(string firmware)
    {
        var tokens = firmware.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length > 1 ? tokens[1].Split('T')[0] : null;
    }

    private sealed record DriveFrame(int Speed, int Angle, bool Accel, bool Brake)
    {
        public static DriveFrame Idle { get; } = new(0, 0, false, false);
    }
}
