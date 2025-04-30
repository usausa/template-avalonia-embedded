namespace Template.EmbeddedApp;

using CommunityToolkit.Mvvm.ComponentModel;

public class MainViewModel : ObservableObject
{
    public Navigator Navigator { get; set; }

    public MainViewModel(Navigator navigator)
    {
        Navigator = navigator;
    }
}
