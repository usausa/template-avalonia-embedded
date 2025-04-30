namespace Template.EmbeddedApp.Views.Main;

using Smart.Avalonia.Input;

public sealed class SubViewModel : AppViewModelBase
{
    public string Message { get; set; }

    public ICommand NavigateCommand { get; }

    public SubViewModel()
    {
        Message = "Hello from SubViewModel!";
        NavigateCommand = new DelegateCommand(() =>
        {
            Navigator.Forward(ViewId.Menu);
        });
    }
}
