namespace Template.EmbeddedApp.Views.Main;

using Avalonia.Threading;

public sealed partial class GraphicsViewModel : AppViewModelBase
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(250);

    private readonly TimeProvider timeProvider;

    private readonly DispatcherTimer timer;

    [ObservableProperty]
    public partial TimeSpan Time { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? Now { get; set; }

    public GraphicsViewModel(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;

        timer = new DispatcherTimer { Interval = RefreshInterval };
        timer.Tick += (_, _) => Refresh();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Stop();
        }

        base.Dispose(disposing);
    }

    public override void OnNavigatedTo(INavigationContext context)
    {
        Refresh();
        timer.Start();
    }

    public override void OnNavigatingFrom(INavigationContext context)
    {
        timer.Stop();
    }

    private void Refresh()
    {
        var now = timeProvider.GetLocalNow();
        Time = new TimeSpan(now.Hour, now.Minute, now.Second);
        Now = now;
    }
}
