namespace Template.EmbeddedApp;

using System;

using Avalonia;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        var builder = BuildAppBuilder();
#if DEBUG
        return builder.StartWithClassicDesktopLifetime(args);
#else
        return builder.StartLinuxDrm(args, "/dev/dri/card1", 1D);
#endif
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAppBuilder() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .WithInterFont()
            .LogToTrace();
}
