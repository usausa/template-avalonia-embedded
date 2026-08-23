namespace Template.EmbeddedApp.Settings;

public sealed class GpioInputSetting
{
    public List<int> Pins { get; } = [];

    public int DebounceMilliseconds { get; set; } = 50;
}
