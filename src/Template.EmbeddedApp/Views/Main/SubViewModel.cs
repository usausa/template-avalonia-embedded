namespace Template.EmbeddedApp.Views.Main;

public sealed partial class SubViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public SubViewModel()
    {
        Message = "Hello from SubViewModel!";
    }
}
