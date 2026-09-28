namespace Template.EmbeddedApp.Devices.BuildHat;

using Template.EmbeddedApp.State;

public enum MotorLink
{
    Disabled,
    Waiting,
    Connected,
    Unsupported
}

public sealed record MotorStatus(MotorPort Port, MotorLink Link, string Name, int Speed, int Position, int? Absolute, int? Angle);

public sealed record DriveStatus(DeviceCondition Condition, string Firmware, double? Voltage, bool PowerFault, MotorStatus Drive, MotorStatus Steering);

public interface IDriveController
{
    DriveStatus GetStatus();

    void Drive(int speed);

    void Steer(int angle);

    void Halt();
}
