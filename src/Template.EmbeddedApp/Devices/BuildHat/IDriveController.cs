namespace Template.EmbeddedApp.Devices.BuildHat;

using Template.EmbeddedApp.State;

public enum MotorLink
{
    Disabled,
    Waiting,
    Connected,
    Unsupported
}

public sealed record MotorStatus(BuildHatPort Port, MotorLink Link, string Name, int Speed, int Position, int? Absolute, int? Angle);

public sealed record BuildHatStatus(DeviceCondition Condition, string Firmware, double? Voltage, bool PowerFault, MotorStatus Drive, MotorStatus Steering);

public interface IDriveController
{
    BuildHatStatus GetStatus();

    void Drive(int speed);

    void Steer(int angle);

    void Halt();
}
