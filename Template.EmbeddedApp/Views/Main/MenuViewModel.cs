namespace Template.EmbeddedApp.Views.Main;

public sealed class MenuViewModel : AppViewModelBase
{
    public string Message { get; set; }

    public MenuViewModel()
    {
        Message = "Hello from MenuViewModel!";
    }

    protected override async ValueTask OnNavigationForwardAsync()
    {
        await Navigator.ForwardAsync(ViewId.Sub);
    }
}
