namespace Template.EmbeddedApp.Views.Main;

public sealed class SubViewModel : AppViewModelBase
{
    public string Message { get; set; }

    public SubViewModel()
    {
        Message = "Hello from SubViewModel!";
    }

    protected override async ValueTask OnNavigationBackAsync()
    {
        await Navigator.ForwardAsync(ViewId.Menu);
    }
}
