namespace Template.EmbeddedApp;

using Template.EmbeddedApp.Devices.Input;

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Input: type=[{type}], profile=[{profile}]")]
    public static partial void InfoStartupInput(this ILogger logger, InputDeviceType type, string profile);

    // Device

    [LoggerMessage(Level = LogLevel.Information, Message = "Device disabled. name=[{name}]")]
    public static partial void InfoDeviceDisabled(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device connected. name=[{name}]")]
    public static partial void InfoDeviceConnected(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Device disconnected. name=[{name}]")]
    public static partial void WarnDeviceDisconnected(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Device error. name=[{name}], message=[{message}]")]
    public static partial void WarnDeviceError(this ILogger logger, string name, string message);

    // Error

    [LoggerMessage(Level = LogLevel.Error, Message = "Unknown exception.")]
    public static partial void ErrorUnknownException(this ILogger logger, Exception ex);
}
