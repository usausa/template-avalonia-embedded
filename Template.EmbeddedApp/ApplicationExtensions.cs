namespace Template.EmbeddedApp;

using System.Runtime.InteropServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

using Smart.Resolver;

using Template.EmbeddedApp.Settings;
using Template.EmbeddedApp.Views;

public static partial class ApplicationExtensions
{
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
        builder.ConfigureContainer(new SmartServiceProviderFactory(), x => ConfigureContainer(builder.Configuration, x));

        return builder;
    }

    private static void ConfigureContainer(ConfigurationManager configuration, ResolverConfig config)
    {
        config
            .UseAutoBinding()
            .UseArrayBinding()
            .UseAssignableBinding();

        // Settings
        config.BindConfig<Setting>(configuration.GetSection("Setting"));

        // Window
        // TODO
        config.BindSingleton<MainView>();
        config.BindSingleton<DebugWindow>();

        // Navigation
        config.BindSingleton<Navigator>(resolver =>
        {
            var navigator = new NavigatorConfig()
                .UseAvaloniaNavigationProvider()
                .UseServiceProvider(resolver)
                .UseIdViewMapper(static m => m.AutoRegister(ViewSource()))
                .ToNavigator();
#if DEBUG
            navigator.Navigated += (_, args) =>
            {
                // for debug
                System.Diagnostics.Debug.WriteLine($"Navigated: [{args.Context.FromId}]->[{args.Context.ToId}] : stacked=[{navigator.StackedCount}]");
            };
#endif

            return navigator;
        });
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    [ViewSource]
    public static partial IEnumerable<KeyValuePair<ViewId, Type>> ViewSource();

    //--------------------------------------------------------------------------------
    // Startup
    //--------------------------------------------------------------------------------

    public static void LogStartupInformation(this IHost host)
    {
        var log = host.Services.GetRequiredService<ILogger<App>>();
        var environment = host.Services.GetRequiredService<IHostEnvironment>();
        ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);

        log.InfoStartup();
        log.InfoStartupSettingsRuntime(RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);
        log.InfoStartupSettingsGC(GCSettings.IsServerGC, GCSettings.LatencyMode, GCSettings.LargeObjectHeapCompactionMode);
        log.InfoStartupSettingsThreadPool(workerThreads, completionPortThreads);
        log.InfoStartupApplication(environment.ApplicationName, typeof(App).Assembly.GetName().Version);
        log.InfoStartupEnvironment(environment.EnvironmentName, environment.ContentRootPath);
    }
}
