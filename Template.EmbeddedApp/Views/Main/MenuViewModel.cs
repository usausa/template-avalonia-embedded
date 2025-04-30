namespace Template.EmbeddedApp.Views.Main;

using Smart.Avalonia.Input;

public sealed class MenuViewModel : AppViewModelBase
{
    public string Message { get; set; }

    public ICommand NavigateCommand { get; }

    public MenuViewModel()
    {
        Message = "Hello from MenuViewModel!";
        NavigateCommand = new DelegateCommand(() =>
        {
            Navigator.Forward(ViewId.Sub);
        });
    }
}
