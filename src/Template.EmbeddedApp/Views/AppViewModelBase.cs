namespace Template.EmbeddedApp.Views;

using Template.EmbeddedApp.Shell;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public abstract class AppViewModelBase :
    ExtendViewModelBase,
    INavigatorAware,
    INavigationEventSupportAsync,
    INavigationLifecycleSupport,
    INotifySupportAsync<NavigationEvent>
{
    public INavigator Navigator { get; set; } = default!;

    protected AppViewModelBase()
    {
        AcceptsCommand = false;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        System.Diagnostics.Debug.WriteLine($"{GetType()} is Disposed");
    }

    public virtual Task OnNavigatingFromAsync(INavigationContext context) => Task.CompletedTask;

    public virtual Task OnNavigatingToAsync(INavigationContext context) => Task.CompletedTask;

    public virtual Task OnNavigatedToAsync(INavigationContext context) => Task.CompletedTask;

    public void OnActivated() => AcceptsCommand = true;

    public void OnDeactivated() => AcceptsCommand = false;

    public async Task NavigatorNotifyAsync(NavigationEvent parameter)
    {
        if (AcceptsCommand)
        {
            switch (parameter)
            {
                case NavigationEvent.Back:
                    await OnNavigationBackAsync();
                    break;
                case NavigationEvent.Forward:
                    await OnNavigationForwardAsync();
                    break;
                case NavigationEvent.Execute:
                    await OnNavigationExecuteAsync();
                    break;
            }
        }
    }

    protected virtual ValueTask OnNavigationBackAsync() => ValueTask.CompletedTask;

    protected virtual ValueTask OnNavigationForwardAsync() => ValueTask.CompletedTask;

    protected virtual ValueTask OnNavigationExecuteAsync() => ValueTask.CompletedTask;
}
