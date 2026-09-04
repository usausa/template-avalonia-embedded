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

        host = CreateHost();

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

    private static IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder();

        // Container
        builder.ConfigureContainer();
        // Log
        builder.ConfigureLogging();
        // Lifetime
        builder.ConfigureLifetime();
        // Components
        builder.ConfigureComponents();

        var host = builder.Build();
#if DEBUG
        if (host.Services is BunnyTail.DependencyInjection.GeneratedServiceProvider generatedProvider)
        {
            foreach (var line in BunnyTail.DependencyInjection.Diagnostics.ServiceFactoryReportExtensions.DescribeRuntimeFallbacks(generatedProvider).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                System.Diagnostics.Debug.WriteLine(line);
            }
        }
#endif
        return host;
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
