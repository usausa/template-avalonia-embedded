namespace Template.EmbeddedApp.Settings;

#pragma warning disable CA1002
public sealed class GpioInputSetting
{
    public List<int> Pins { get; } = [];

    public int DebounceMilliseconds { get; set; } = 50;
}
#pragma warning restore CA1002
