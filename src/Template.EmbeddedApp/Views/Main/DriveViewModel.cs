namespace Template.EmbeddedApp.Views.Main;

using Avalonia.Threading;

using Template.EmbeddedApp.Devices.BuildHat;
using Template.EmbeddedApp.Devices.Input;
using Template.EmbeddedApp.State;

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

    public string DriveTitle { get; }

    public string SteeringTitle { get; }

    public int MinAngle { get; }

    public int MaxAngle { get; }

    [ObservableProperty]
    public partial string HatState { get; set; } = "-";

    [ObservableProperty]
    public partial bool IsHatConnected { get; set; }

    [ObservableProperty]
    public partial bool IsPowerFault { get; set; }

    [ObservableProperty]
    public partial string HatDetail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DriveName { get; set; } = "-";

    [ObservableProperty]
    public partial bool IsDriveConnected { get; set; }

    [ObservableProperty]
    public partial bool IsDriveUnsupported { get; set; }

    [ObservableProperty]
    public partial string Direction { get; set; } = "Stopped";

    [ObservableProperty]
    public partial int TargetSpeed { get; set; }

    [ObservableProperty]
    public partial string TargetSpeedText { get; set; } = "0 %";

    [ObservableProperty]
    public partial int ActualSpeed { get; set; }

    [ObservableProperty]
    public partial string ActualSpeedText { get; set; } = "-";

    [ObservableProperty]
    public partial string DrivePosition { get; set; } = "-";

    [ObservableProperty]
    public partial string SteeringName { get; set; } = "-";

    [ObservableProperty]
    public partial bool IsSteeringConnected { get; set; }

    [ObservableProperty]
    public partial bool IsSteeringUnsupported { get; set; }

    [ObservableProperty]
    public partial string SteeringState { get; set; } = "Center";

    [ObservableProperty]
    public partial int TargetAngle { get; set; }

    [ObservableProperty]
    public partial string TargetAngleText { get; set; } = "0 deg";

    [ObservableProperty]
    public partial int ActualAngle { get; set; }

    [ObservableProperty]
    public partial string ActualAngleText { get; set; } = "-";

    [ObservableProperty]
    public partial string SteeringAbsolute { get; set; } = "-";

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
        DriveTitle = String.Create(CultureInfo.InvariantCulture, $"{option.Drive.Port}  Drive");
        SteeringTitle = String.Create(CultureInfo.InvariantCulture, $"{option.Steering.Port}  Steering");
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
                accel = input.IsPressed(InputKey.Button2);
                brake = input.IsPressed(InputKey.Button1);
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
        var culture = CultureInfo.InvariantCulture;
        var hat = driveController.GetStatus();
        var current = frame;

        HatState = hat.PowerFault ? "Power fault" : hat.Condition.ToString();
        IsHatConnected = hat.Condition == DeviceCondition.Connected;
        IsPowerFault = hat.PowerFault;
        HatDetail = FormatDetail(hat);

        DriveName = FormatName(hat.Drive);
        IsDriveConnected = hat.Drive.Link == MotorLink.Connected;
        IsDriveUnsupported = hat.Drive.Link == MotorLink.Unsupported;
        Direction = current.Speed > 0 ? "Forward" : "Stopped";
        TargetSpeed = current.Speed;
        TargetSpeedText = String.Create(culture, $"{current.Speed} %");
        ActualSpeed = IsDriveConnected ? Math.Abs(hat.Drive.Speed) : 0;
        ActualSpeedText = IsDriveConnected ? String.Create(culture, $"{hat.Drive.Speed} %") : "-";
        DrivePosition = IsDriveConnected ? String.Create(culture, $"{hat.Drive.Position} deg") : "-";

        SteeringName = FormatName(hat.Steering);
        IsSteeringConnected = hat.Steering.Link == MotorLink.Connected;
        IsSteeringUnsupported = hat.Steering.Link == MotorLink.Unsupported;
        SteeringState = current.Angle switch
        {
            < 0 => "Left",
            > 0 => "Right",
            _ => "Center"
        };
        TargetAngle = current.Angle;
        TargetAngleText = String.Create(culture, $"{current.Angle} deg");
        ActualAngle = IsSteeringConnected ? (hat.Steering.Angle ?? 0) : 0;
        ActualAngleText = IsSteeringConnected && (hat.Steering.Angle is { } angle) ? String.Create(culture, $"{angle} deg") : "-";
        SteeringAbsolute = IsSteeringConnected && (hat.Steering.Absolute is { } absolute) ? String.Create(culture, $"{absolute} deg") : "-";

        Accel = current.Accel;
        Brake = current.Brake;
    }

    private static string FormatName(MotorStatus motor) => motor.Link switch
    {
        MotorLink.Disabled => "Disabled",
        MotorLink.Waiting => "Not connected",
        MotorLink.Unsupported => String.Create(CultureInfo.InvariantCulture, $"Unsupported: {motor.Name}"),
        _ => motor.Name
    };

    private static string FormatDetail(BuildHatStatus hat)
    {
        var parts = new List<string>();
        if (hat.Voltage is { } voltage)
        {
            parts.Add(String.Create(CultureInfo.InvariantCulture, $"{voltage:F1} V"));
        }

        var tokens = hat.Firmware.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length > 1)
        {
            parts.Add("fw " + tokens[1].Split('T')[0]);
        }

        return String.Join("  ", parts);
    }

    private sealed record DriveFrame(int Speed, int Angle, bool Accel, bool Brake)
    {
        public static DriveFrame Idle { get; } = new(0, 0, false, false);
    }
}
