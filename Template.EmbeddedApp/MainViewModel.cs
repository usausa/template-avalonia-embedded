namespace Template.EmbeddedApp;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class MainViewModel : ObservableObject
{
    public string Message { get; }

    public MainViewModel()
    {
        Message = "Hello, Avalonia!";
    }
}
