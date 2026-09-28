namespace Template.EmbeddedApp;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Smart.Mvvm.Resolver;

using Template.EmbeddedApp.Settings;

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

        // Setup navigator
        var navigator = host.Services.GetRequiredService<INavigator>();
        navigator.Navigated += (_, args) =>
        {
            // for debug
            System.Diagnostics.Debug.WriteLine($"Navigated: [{args.Context.FromId}]->[{args.Context.ToId}] : stacked=[{navigator.StackedCount}]");
        };
#endif
        return host;
    }

    // ReSharper disable once AsyncVoidMethod
    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform and IControlledApplicationLifetime controlled)
        {
            // Main view
            var mainView = host.Services.GetRequiredService<MainView>();
            singleViewPlatform.MainView = mainView;
            LogDisplay(mainView);

            // Stop request
            host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() => Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    await host.ExitApplicationAsync();
                }
                finally
                {
                    controlled.Shutdown();
                }
            }));

            // Start
            await host.StartApplicationAsync();
        }
        else if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Debug window
            var window = host.Services.GetRequiredService<DebugWindow>();

            // Stop request
            var stopping = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() => Dispatcher.UIThread.Post(window.Close));

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            window.Closed += async (_, _) =>
            {
                try
                {
                    await stopping.DisposeAsync();
                    await host.ExitApplicationAsync();
                }
                finally
                {
                    desktop.Shutdown();
                }
            };
            desktop.MainWindow = window;

            // Start
            await host.StartApplicationAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void LogDisplay(Control view)
    {
        if (TopLevel.GetTopLevel(view) is not { } topLevel)
        {
            return;
        }

        var log = host.Services.GetRequiredService<ILogger<App>>();
        var display = host.Services.GetRequiredService<DisplaySetting>();
        var size = PixelSize.FromSize(topLevel.ClientSize, topLevel.RenderScaling);
        log.InfoStartupDisplay(size.Width, size.Height, topLevel.RenderScaling);
        if ((size.Width != display.Width) || (size.Height != display.Height))
        {
            log.WarnStartupDisplaySize(size.Width, size.Height, display.Width, display.Height);
        }
    }
}
