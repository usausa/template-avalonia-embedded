namespace Template.EmbeddedApp;

using Smart.Avalonia.ViewModels;

using Template.EmbeddedApp.Devices.Input;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public class DebugWindowViewModel : ExtendViewModelBase
{
    public ICommand BackCommand { get; }

    public ICommand NextCommand { get; }

    public DebugWindowViewModel(DebugInputDevice input)
    {
        NextCommand = MakeDelegateCommand(() => input.Trigger(InputKey.Button1));
        BackCommand = MakeDelegateCommand(() => input.Trigger(InputKey.Button2));
    }
}
