namespace Template.EmbeddedApp.Views;

using CommunityToolkit.Mvvm.ComponentModel;

// TODO
public abstract class AppViewModelBase : ObservableObject, INavigatorAware, INavigationEventSupport
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
}
