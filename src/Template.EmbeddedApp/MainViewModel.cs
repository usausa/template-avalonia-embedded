namespace Template.EmbeddedApp;

using System.Reactive.Concurrency;

using Smart.Avalonia.ViewModels;

using Template.EmbeddedApp.Devices.Input;
using Template.EmbeddedApp.Shell;
using Template.EmbeddedApp.Views;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public class MainViewModel : ExtendViewModelBase
{
    private static readonly ViewId[] Views =
    [
        ViewId.Menu,
        ViewId.Sub,
        ViewId.Drive
    ];

    public INavigator Navigator { get; set; }

    public MainViewModel(INavigator navigator, IInputDevice input)
    {
        Navigator = navigator;

        var scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
        Disposables.Add(Observable
            .FromEvent<EventHandler<EventArgs<InputSignal>>, EventArgs<InputSignal>>(static h => (_, e) => h(e), h => input.Handle += h, h => input.Handle -= h)
            .ObserveOn(scheduler)
            .Select(x => Observable.FromAsync(() => HandleInputAsync(x.Data), scheduler))
            .Concat()
            .Subscribe());
    }

    private Task HandleInputAsync(InputSignal signal) => signal switch
    {
        { Key: InputKey.Select, Action: InputAction.Press } => SwitchViewAsync(),
        { Key: InputKey.Button1, Action: InputAction.Press } => Navigator.NotifyAsync(NavigationEvent.Forward),
        { Key: InputKey.Button2, Action: InputAction.Press } => Navigator.NotifyAsync(NavigationEvent.Back),
        { Key: InputKey.Button3, Action: InputAction.Press } => Navigator.NotifyAsync(NavigationEvent.Execute),
        { Key: InputKey.Button4, Action: InputAction.LongPress } => ShowStatusAsync(),
        _ => Task.CompletedTask
    };

    private Task<bool> SwitchViewAsync()
    {
        if (Navigator.CurrentViewId is ViewId.Status)
        {
            return Navigator.PopAsync();
        }

        var index = Navigator.CurrentViewId is ViewId current ? Array.IndexOf(Views, current) : -1;
        return Navigator.ForwardAsync(Views[(index + 1) % Views.Length]);
    }

    private Task ShowStatusAsync() =>
        Navigator.CurrentViewId is ViewId.Status ? Task.CompletedTask : Navigator.PushAsync(ViewId.Status);
}
