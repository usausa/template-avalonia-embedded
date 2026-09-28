namespace Template.EmbeddedApp;

using Template.EmbeddedApp.Devices.Input;

public sealed partial class DebugKey : ObservableObject
{
    public InputKey Key { get; }

    public string Label { get; }

    [ObservableProperty]
    public partial bool IsPressed { get; set; }

    public DebugKey(InputKey key, int number)
    {
        Key = key;
        Label = String.Create(CultureInfo.InvariantCulture, $"{number}: {key}");
    }
}
