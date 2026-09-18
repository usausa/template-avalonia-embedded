namespace Template.EmbeddedApp;

using System.Reactive.Concurrency;

using Smart.Avalonia.ViewModels;

using Template.EmbeddedApp.Devices.Input;
using Template.EmbeddedApp.Shell;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public class MainViewModel : ExtendViewModelBase
{
    public INavigator Navigator { get; set; }

    public MainViewModel(INavigator navigator, IInputDevice input)
    {
        Navigator = navigator;

        var scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
        Disposables.Add(Observable
            .FromEvent<EventHandler<EventArgs<InputKey>>, EventArgs<InputKey>>(static h => (_, e) => h(e), h => input.Handle += h, h => input.Handle -= h)
            .ObserveOn(scheduler)
            .Select(x => Observable.FromAsync(() => HandleInputAsync(x.Data), scheduler))
            .Concat()
            .Subscribe());
    }

    private Task HandleInputAsync(InputKey key) => key switch
    {
        InputKey.Button1 => Navigator.NotifyAsync(NavigationEvent.Forward),
        InputKey.Button2 => Navigator.NotifyAsync(NavigationEvent.Back),
        _ => Task.CompletedTask
    };
}
