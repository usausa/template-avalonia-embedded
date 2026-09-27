namespace Template.EmbeddedApp;

using Smart.Avalonia.ViewModels;

using Template.EmbeddedApp.Devices.Input;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public class DebugWindowViewModel : ExtendViewModelBase
{
    public ICommand SelectCommand { get; }

    public ICommand StatusCommand { get; }

    public ICommand AccelCommand { get; }

    public ICommand BrakeCommand { get; }

    public ICommand LeftCommand { get; }

    public ICommand RightCommand { get; }

    public DebugWindowViewModel(DebugInputDevice input)
    {
        SelectCommand = MakeDelegateCommand(() => input.Trigger(InputKey.Select));
        StatusCommand = MakeDelegateCommand(() => input.LongPress(InputKey.Button4));
        AccelCommand = MakeDelegateCommand<bool?>(x => input.Hold(InputKey.Button2, x == true));
        BrakeCommand = MakeDelegateCommand<bool?>(x => input.Hold(InputKey.Button1, x == true));
        LeftCommand = MakeDelegateCommand<bool?>(x => input.Hold(InputKey.Left, x == true));
        RightCommand = MakeDelegateCommand<bool?>(x => input.Hold(InputKey.Right, x == true));
    }
}
