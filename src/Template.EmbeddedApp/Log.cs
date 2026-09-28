namespace Template.EmbeddedApp;

using RaspberryDotNet.BuildHat;

internal static partial class Log
{
    // Startup

    [LoggerMessage(Level = LogLevel.Information, Message = "Application start.")]
    public static partial void InfoStartup(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Runtime: os=[{osDescription}], framework=[{frameworkDescription}], rid=[{runtimeIdentifier}]")]
    public static partial void InfoStartupSettingsRuntime(this ILogger logger, string osDescription, string frameworkDescription, string runtimeIdentifier);

    [LoggerMessage(Level = LogLevel.Information, Message = "GCSettings: serverGC=[{isServerGC}], latencyMode=[{latencyMode}], largeObjectHeapCompactionMode=[{largeObjectHeapCompactionMode}]")]
    public static partial void InfoStartupSettingsGC(this ILogger logger, bool isServerGC, GCLatencyMode latencyMode, GCLargeObjectHeapCompactionMode largeObjectHeapCompactionMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "ThreadPool: workerThreads=[{workerThreads}], completionPortThreads=[{completionPortThreads}]")]
    public static partial void InfoStartupSettingsThreadPool(this ILogger logger, int workerThreads, int completionPortThreads);

    [LoggerMessage(Level = LogLevel.Information, Message = "Application: application=[{application}], version=[{version}]")]
    public static partial void InfoStartupApplication(this ILogger logger, string application, Version? version);

    [LoggerMessage(Level = LogLevel.Information, Message = "Environment: environment=[{environment}], contentRoot=[{contentRoot}]")]
    public static partial void InfoStartupEnvironment(this ILogger logger, string environment, string contentRoot);

    [LoggerMessage(Level = LogLevel.Information, Message = "Input: device=[{device}], profile=[{profile}]")]
    public static partial void InfoStartupInput(this ILogger logger, string device, string profile);

    [LoggerMessage(Level = LogLevel.Information, Message = "Display: size=[{width}x{height}], scaling=[{scaling}]")]
    public static partial void InfoStartupDisplay(this ILogger logger, int width, int height, double scaling);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Display size differs from the setting. size=[{width}x{height}], setting=[{settingWidth}x{settingHeight}]")]
    public static partial void WarnStartupDisplaySize(this ILogger logger, int width, int height, int settingWidth, int settingHeight);

    // Device

    [LoggerMessage(Level = LogLevel.Information, Message = "Device disabled. name=[{name}]")]
    public static partial void InfoDeviceDisabled(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device connected. name=[{name}]")]
    public static partial void InfoDeviceConnected(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Device disconnected. name=[{name}]")]
    public static partial void WarnDeviceDisconnected(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Device error. name=[{name}], message=[{message}]")]
    public static partial void WarnDeviceError(this ILogger logger, string name, string message);

    // Build HAT

    [LoggerMessage(Level = LogLevel.Information, Message = "Build HAT opened. firmware=[{firmware}], loaded=[{loaded}]")]
    public static partial void InfoBuildHatOpened(this ILogger logger, string firmware, bool loaded);

    [LoggerMessage(Level = LogLevel.Information, Message = "Build HAT port connected. port=[{port}], device=[{device}], support=[{support}]")]
    public static partial void InfoBuildHatPortConnected(this ILogger logger, char port, string device, BuildHatDeviceSupport support);

    [LoggerMessage(Level = LogLevel.Information, Message = "Build HAT port disconnected. port=[{port}]")]
    public static partial void InfoBuildHatPortDisconnected(this ILogger logger, char port);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Build HAT power fault. fault=[{fault}]")]
    public static partial void WarnBuildHatPowerFault(this ILogger logger, BuildHatFault fault);

    // Capture

    [LoggerMessage(Level = LogLevel.Information, Message = "Screen captured. path=[{path}]")]
    public static partial void InfoScreenCaptured(this ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Screen capture failed. path=[{path}]")]
    public static partial void WarnScreenCaptureFailed(this ILogger logger, Exception ex, string path);

    // Error

    [LoggerMessage(Level = LogLevel.Error, Message = "Unknown exception.")]
    public static partial void ErrorUnknownException(this ILogger logger, Exception ex);
}
