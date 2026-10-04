namespace Template.EmbeddedApp;

using System.Runtime.InteropServices;

using BunnyTail.DependencyInjection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Serilog;

using Smart.Avalonia;

using Template.EmbeddedApp.Devices.BuildHat;
using Template.EmbeddedApp.Devices.Input;
using Template.EmbeddedApp.Settings;
using Template.EmbeddedApp.Shell;
using Template.EmbeddedApp.State;
using Template.EmbeddedApp.Views;

public static partial class ApplicationExtensions
{
    //--------------------------------------------------------------------------------
    // Container
    //--------------------------------------------------------------------------------

    public static HostApplicationBuilder ConfigureContainer(this HostApplicationBuilder builder)
    {
        builder.ConfigureContainer(new GeneratedServiceProviderFactory(static options => options.TrackTransientDisposables = false));

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Logging
    //--------------------------------------------------------------------------------

    public static HostApplicationBuilder ConfigureLogging(this HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog(options =>
        {
            options.ReadFrom.Configuration(builder.Configuration);
        });

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Components
    //--------------------------------------------------------------------------------

    public static HostApplicationBuilder ConfigureComponents(this HostApplicationBuilder builder)
    {
        builder.Services.AddAvaloniaServices();

        // System
        builder.Services.AddSingleton(TimeProvider.System);

        // Setting
        builder.Services.AddOptions<Setting>().BindConfiguration("Setting").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<Setting>>().Value);
        builder.Services.AddSingleton(builder.Configuration.GetSection("Display").Get<DisplaySetting>() ?? new DisplaySetting());
        builder.Services.AddSingleton(builder.Configuration.GetSection("Capture").Get<CaptureSetting>() ?? new CaptureSetting());

        // Messenger
        builder.Services.AddSingleton<IReactiveMessenger>(ReactiveMessenger.Default);

        // Navigation
        builder.Services.AddNavigator(static (_, config) =>
        {
            config.UseAvaloniaNavigationProvider();
            config.UseIdViewMapper(static m => m.AutoRegister(ViewSource()));
        });

        // Capture
        builder.Services.AddSingleton<ScreenCapture>();

        // Device
        builder.Services.AddSingleton<DeviceState>();
        builder.Services.AddOptions<InputOption>().BindConfiguration("Input").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<InputOption>>().Value);
#if DEBUG
        builder.Services.AddSingleton<DebugInputDevice>();
        builder.Services.AddSingleton<IInputDevice>(static p => p.GetRequiredService<DebugInputDevice>());
#else
        builder.Services.AddSingleton<IInputDevice, PadInputDevice>();
#endif

        builder.Services.AddOptions<BuildHatOption>().BindConfiguration("BuildHat").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<BuildHatOption>>().Value);
        builder.Services.AddSingleton<IDriveController, BuildHatDriveController>();

        // Service
        builder.Services.AddServices();

        // Window
        builder.Services.AddSingleton<MainView>();
#if DEBUG
        builder.Services.AddSingleton<DebugWindow>();
#endif
        // View & ViewModel
        builder.Services.AddViews();
        builder.Services.AddViewModels();

        return builder;
    }
    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    [ViewSource]
    public static partial IEnumerable<KeyValuePair<ViewId, Type>> ViewSource();

    //--------------------------------------------------------------------------------
    // Service
    //--------------------------------------------------------------------------------

    [ComponentRegistration(Lifetime.Singleton, "Service$")]
    public static partial IServiceCollection AddServices(this IServiceCollection services);

    //--------------------------------------------------------------------------------
    // View & ViewModel
    //--------------------------------------------------------------------------------

    [ComponentRegistration(Lifetime.Transient, "View$", Namespace = "Template.EmbeddedApp.Views")]
    public static partial IServiceCollection AddViews(this IServiceCollection services);

    [ComponentRegistration(Lifetime.Transient, "ViewModel$")]
    public static partial IServiceCollection AddViewModels(this IServiceCollection services);

    //--------------------------------------------------------------------------------
    // Startup
    //--------------------------------------------------------------------------------

    public static async ValueTask StartApplicationAsync(this IHost host)
    {
        // Start host
        await host.StartAsync().ConfigureAwait(false);

        // Startup log
        var log = host.Services.GetRequiredService<ILogger<App>>();
        var environment = host.Services.GetRequiredService<IHostEnvironment>();
        var input = host.Services.GetRequiredService<InputOption>();
        ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);

        log.InfoStartup();
        log.InfoStartupSettingsRuntime(RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);
        log.InfoStartupSettingsGC(GCSettings.IsServerGC, GCSettings.LatencyMode, GCSettings.LargeObjectHeapCompactionMode);
        log.InfoStartupSettingsThreadPool(workerThreads, completionPortThreads);
        log.InfoStartupApplication(environment.ApplicationName, typeof(App).Assembly.GetName().Version);
        log.InfoStartupEnvironment(environment.EnvironmentName, environment.ContentRootPath);
        log.InfoStartupInput(input.PadDevice, input.Profile);

        // Device
        host.Services.GetRequiredService<IDriveController>();

        // Navigate to view
        var navigator = host.Services.GetRequiredService<INavigator>();
        await navigator.ForwardAsync(ViewId.Dashboard).ConfigureAwait(false);
    }

    public static async ValueTask ExitApplicationAsync(this IHost host)
    {
        // Stop host
        await host.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        host.Dispose();
    }
}
