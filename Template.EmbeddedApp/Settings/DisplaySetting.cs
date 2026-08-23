namespace Template.EmbeddedApp.Settings;

public sealed class DisplaySetting
{
    public string Device { get; set; } = "/dev/dri/card1";

    public double Scaling { get; set; } = 1d;
}
