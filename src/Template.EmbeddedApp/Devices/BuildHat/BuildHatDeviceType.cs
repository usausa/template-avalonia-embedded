namespace Template.EmbeddedApp.Devices.BuildHat;

public sealed record BuildHatDeviceType(string Name, bool IsMotor, bool HasAbsolutePosition)
{
    private static readonly Dictionary<int, BuildHatDeviceType> ActiveTypes = new()
    {
        [0x22] = new BuildHatDeviceType("Tilt Sensor", false, false),
        [0x23] = new BuildHatDeviceType("Motion Sensor", false, false),
        [0x25] = new BuildHatDeviceType("Color & Distance Sensor", false, false),
        [0x26] = new BuildHatDeviceType("Medium Linear Motor", true, false),
        [0x2E] = new BuildHatDeviceType("Technic Large Motor", true, true),
        [0x2F] = new BuildHatDeviceType("Technic XL Motor", true, true),
        [0x30] = new BuildHatDeviceType("Technic Medium Angular Motor", true, true),
        [0x31] = new BuildHatDeviceType("Technic Large Angular Motor", true, true),
        [0x3D] = new BuildHatDeviceType("Color Sensor", false, false),
        [0x3E] = new BuildHatDeviceType("Distance Sensor", false, false),
        [0x3F] = new BuildHatDeviceType("Force Sensor", false, false),
        [0x40] = new BuildHatDeviceType("Color Light Matrix", false, false),
        [0x41] = new BuildHatDeviceType("Small Angular Motor", true, true),
        [0x4B] = new BuildHatDeviceType("Technic Medium Angular Motor (Grey)", true, true),
        [0x4C] = new BuildHatDeviceType("Technic Large Angular Motor (Grey)", true, true)
    };

    public static BuildHatDeviceType Find(int id, bool active) =>
        active && ActiveTypes.TryGetValue(id, out var type)
            ? type
            : new BuildHatDeviceType(String.Create(CultureInfo.InvariantCulture, $"{(active ? "Active" : "Passive")} device 0x{id:X2}"), false, false);
}
