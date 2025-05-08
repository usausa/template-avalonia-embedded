namespace Template.EmbeddedApp;

using Smart.Avalonia.ViewModels;

using Template.EmbeddedApp.Devices.Input;
using Template.EmbeddedApp.Shell;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public class MainViewModel : ExtendViewModelBase
{
    public Navigator Navigator { get; set; }

    public MainViewModel(Navigator navigator, IInputDevice input)
    {
        Navigator = navigator;

        // ReSharper disable once AsyncVoidMethod
        Disposables.Add(Observable
            .FromEvent<EventHandler<EventArgs<InputKey>>, EventArgs<InputKey>>(static h => (_, e) => h(e), h => input.Handle += h, h => input.Handle -= h)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(async void (x) =>
            {
                switch (x.Data)
                {
                    case InputKey.Button1:
                        await Navigator.NotifyAsync(NavigationEvent.Forward);
                        break;
                    case InputKey.Button2:
                        await Navigator.NotifyAsync(NavigationEvent.Back);
                        break;
                }
            }));
    }
}
