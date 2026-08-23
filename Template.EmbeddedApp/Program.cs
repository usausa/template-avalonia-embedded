namespace Template.EmbeddedApp;

using System;

using Avalonia;

#if !DEBUG
using Microsoft.Extensions.Configuration;

using Template.EmbeddedApp.Settings;
#endif

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        var builder = BuildAvaloniaApp();
#if DEBUG
        return builder.StartWithClassicDesktopLifetime(args);
#else
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        var display = configuration.GetSection("Display").Get<DisplaySetting>() ?? new DisplaySetting();
        return builder.StartLinuxDrm(args, display.Device, display.Scaling);
#endif
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .WithInterFont()
            .LogToTrace();
}
