namespace Template.EmbeddedApp.Views;

using Template.EmbeddedApp.Shell;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public abstract class AppViewModelBase : ExtendViewModelBase, INavigatorAware, INavigationEventSupport, INotifySupportAsync<NavigationEvent>
{
    public INavigator Navigator { get; set; } = default!;

    public void OnNavigatingFrom(INavigationContext context)
    {
    }

    public void OnNavigatingTo(INavigationContext context)
    {
    }

    public void OnNavigatedTo(INavigationContext context)
    {
    }

    public async Task NavigatorNotifyAsync(NavigationEvent parameter)
    {
        switch (parameter)
        {
            case NavigationEvent.Back:
                await OnNavigationBackAsync();
                break;
            case NavigationEvent.Forward:
                await OnNavigationForwardAsync();
                break;
        }
    }

    protected virtual ValueTask OnNavigationBackAsync() => ValueTask.CompletedTask;

    protected virtual ValueTask OnNavigationForwardAsync() => ValueTask.CompletedTask;
}
