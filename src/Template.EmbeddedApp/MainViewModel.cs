namespace Template.EmbeddedApp;

using System.Reactive.Concurrency;

using Smart.Avalonia.ViewModels;

using Template.EmbeddedApp.Devices.Input;
using Template.EmbeddedApp.Shell;
using Template.EmbeddedApp.Views;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public class MainViewModel : ExtendViewModelBase
{
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
        { Key: InputKey.Button1, Action: InputAction.Press } => Navigator.NotifyAsync(NavigationEvent.Forward),
        { Key: InputKey.Button2, Action: InputAction.Press } => Navigator.NotifyAsync(NavigationEvent.Back),
        { Key: InputKey.Button4, Action: InputAction.LongPress } => ShowStatusAsync(),
        _ => Task.CompletedTask
    };

    private Task ShowStatusAsync() =>
        Navigator.CurrentViewId is ViewId.Status ? Task.CompletedTask : Navigator.PushAsync(ViewId.Status);
}
