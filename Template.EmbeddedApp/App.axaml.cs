namespace Template.EmbeddedApp;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Smart.Mvvm.Resolver;

// ReSharper disable once PartialTypeWithSinglePart
public partial class App : Application
{
    private IHost host = default!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif

        host = Host.CreateApplicationBuilder()
            .ConfigureLogging()
            .ConfigureLifetime()
            .ConfigureComponents()
            .Build();
        ResolveProvider.Default.Provider = host.Services;

        // Exception hook
        var log = host.Services.GetRequiredService<ILogger<App>>();
        AppDomain.CurrentDomain.UnhandledException += (_, args) => log.ErrorUnknownException((Exception)args.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            log.ErrorUnknownException(args.Exception);
            args.SetObserved();
        };
    }

    // ReSharper disable once AsyncVoidMethod
    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // Main view
            singleViewPlatform.MainView = host.Services.GetRequiredService<MainView>();

            // Start
            await host.StartApplicationAsync();
        }
        else if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Exit hook
            desktop.Exit += async (_, _) => await host.ExitApplicationAsync();

            // Debug window
            desktop.MainWindow = host.Services.GetRequiredService<DebugWindow>();

            // Start
            await host.StartApplicationAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
