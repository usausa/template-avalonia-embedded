namespace Template.EmbeddedApp;

using System.Runtime.InteropServices;

using BunnyTail.DependencyInjection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Serilog;

using Smart.Avalonia;

using Template.EmbeddedApp.Devices.Input;
using Template.EmbeddedApp.Settings;
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
    // Lifetime
    //--------------------------------------------------------------------------------

    public static HostApplicationBuilder ConfigureLifetime(this HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IHostLifetime, NopLifetime>();

        return builder;
    }

#pragma warning disable CA1812
    private sealed class NopLifetime : IHostLifetime
    {
        public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
#pragma warning restore CA1812

    //--------------------------------------------------------------------------------
    // Components
    //--------------------------------------------------------------------------------

    public static HostApplicationBuilder ConfigureComponents(this HostApplicationBuilder builder)
    {
        builder.Services.AddAvaloniaServices();

        // System
        builder.Services.AddSingleton(TimeProvider.System);

        // Setting
        builder.Services.AddSingleton(builder.Configuration.GetSection("Setting").Get<Setting>() ?? new Setting());

        // Messenger
        builder.Services.AddSingleton<IReactiveMessenger>(ReactiveMessenger.Default);

        // Navigation
        builder.Services.AddNavigator(static (_, config) =>
        {
            config.UseAvaloniaNavigationProvider();
            config.UseIdViewMapper(static m => m.AutoRegister(ViewSource()));
        });

        // Device
        builder.Services.AddSingleton<DeviceState>();
        builder.Services.AddOptions<InputOption>().BindConfiguration("Input").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<InputOption>>().Value);
#if DEBUG
        builder.Services.AddSingleton<DebugInputDevice>();
        builder.Services.AddSingleton<IInputDevice>(static p => p.GetRequiredService<DebugInputDevice>());
#else
        if (builder.Configuration.GetValue<InputDeviceType>("Input:Type") == InputDeviceType.Gpio)
        {
            builder.Services.AddSingleton<IInputDevice, GpioInputDevice>();
        }
        else
        {
            builder.Services.AddSingleton<IInputDevice, PadInputDevice>();
        }
#endif

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
        log.InfoStartupInput(input.Type, input.Profile);

        // Navigate to view
        var navigator = host.Services.GetRequiredService<INavigator>();
        await navigator.ForwardAsync(ViewId.Menu).ConfigureAwait(false);
    }

    public static async ValueTask ExitApplicationAsync(this IHost host)
    {
        // Stop host
        await host.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        host.Dispose();
    }
}
