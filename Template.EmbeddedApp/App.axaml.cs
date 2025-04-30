namespace Template.EmbeddedApp;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Smart.Avalonia.Resolver;

using Template.EmbeddedApp.Views;

// ReSharper disable once PartialTypeWithSinglePart
public partial class App : Application
{
    private IHost host = default!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        host = Host.CreateApplicationBuilder()
            .ConfigureLogging()
            .ConfigureComponents()
            .Build();
        ResolveProvider.Default.Provider = host.Services;
    }

    // ReSharper disable once AsyncVoidMethod
    public override async void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // TODO
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Stop host hook
            desktop.Exit += async (_, _) =>
            {
                await host.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                host.Dispose();
            };

            // Debug window
            desktop.MainWindow = host.Services.GetRequiredService<DebugWindow>();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            //singleViewPlatform.MainView = new MainView
            //{
            //    DataContext = new MainViewModel()
            //};
        }

        base.OnFrameworkInitializationCompleted();

        // Start host
        await host.StartAsync().ConfigureAwait(false);

        // Startup log
        host.LogStartupInformation();

        // Navigate to view
        var navigator = host.Services.GetRequiredService<Navigator>();
        await navigator.ForwardAsync(ViewId.Menu);
    }
}
