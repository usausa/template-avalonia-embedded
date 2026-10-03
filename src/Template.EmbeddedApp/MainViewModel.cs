namespace Template.EmbeddedApp;

using System.Reactive.Concurrency;

using Smart.Avalonia.ViewModels;
using Smart.Mvvm.ViewModels;

using Template.EmbeddedApp.Devices.Input;
using Template.EmbeddedApp.Shell;
using Template.EmbeddedApp.Views;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public class MainViewModel : ExtendViewModelBase
{
    private static readonly ViewId[] Views =
    [
        ViewId.Dashboard,
        ViewId.Monitor,
        ViewId.Gpio,
        ViewId.Input,
        ViewId.Drive,
        ViewId.Graphics,
        ViewId.Typography
    ];

    private readonly ScreenCapture screenCapture;

    private IDisposable? navigatingBusy;

    public INavigator Navigator { get; set; }

    public MainViewModel(INavigator navigator, IInputDevice input, ScreenCapture screenCapture)
    {
        Navigator = navigator;
        this.screenCapture = screenCapture;

        // Busy while navigating
        Disposables.Add(Observable.FromEventPattern<EventArgs>(h => Navigator.ExecutingChanged += h, h => Navigator.ExecutingChanged -= h)
            .Subscribe(_ => UpdateNavigatingBusy()));

        var scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
        Disposables.Add(Observable
            .FromEvent<EventHandler<EventArgs<InputSignal>>, EventArgs<InputSignal>>(static h => (_, e) => h(e), h => input.Handle += h, h => input.Handle -= h)
            .ObserveOn(scheduler)
            .Select(x => Observable.FromAsync(() => HandleInputAsync(x.Data), scheduler))
            .Concat()
            .Subscribe());
    }

    private void UpdateNavigatingBusy()
    {
        if (Navigator.Executing)
        {
            navigatingBusy ??= BusyState.Begin();
        }
        else
        {
            navigatingBusy?.Dispose();
            navigatingBusy = null;
        }
    }

    private Task HandleInputAsync(InputSignal signal) => signal switch
    {
        { Key: InputKey.Select, Action: InputAction.Press } => SwitchViewAsync(),
        { Key: InputKey.Button1, Action: InputAction.Press } => Navigator.NotifyAsync(NavigationEvent.Forward),
        { Key: InputKey.Button2, Action: InputAction.Press } => Navigator.NotifyAsync(NavigationEvent.Back),
        { Key: InputKey.Button3, Action: InputAction.Press } => Navigator.NotifyAsync(NavigationEvent.Execute),
        { Key: InputKey.Capture, Action: InputAction.Press } => screenCapture.CaptureAsync(),
        _ => Task.CompletedTask
    };

    private Task<bool> SwitchViewAsync()
    {
        var index = Navigator.CurrentViewId is ViewId current ? Array.IndexOf(Views, current) : -1;
        return Navigator.ForwardAsync(Views[(index + 1) % Views.Length]);
    }
}
