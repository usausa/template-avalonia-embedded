namespace Template.EmbeddedApp.Shell;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

using Template.EmbeddedApp.Settings;

public sealed class ScreenCapture
{
    private readonly ILogger<ScreenCapture> log;

    private readonly TimeProvider timeProvider;

    private readonly INavigator navigator;

    private readonly CaptureSetting setting;

    public ScreenCapture(ILogger<ScreenCapture> log, TimeProvider timeProvider, INavigator navigator, CaptureSetting setting)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        this.navigator = navigator;
        this.setting = setting;
    }

    public async Task<string?> CaptureAsync()
    {
        if ((navigator.CurrentView as Visual)?.FindAncestorOfType<MainView>() is not { } target)
        {
            return null;
        }

        var name = String.Create(CultureInfo.InvariantCulture, $"{timeProvider.GetLocalNow():yyyyMMdd-HHmmss-fff}_{navigator.CurrentViewId}.png");
        var path = Path.GetFullPath(Path.Combine(setting.Directory, name));
        try
        {
            var image = Render(target);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, image).ConfigureAwait(false);
            log.InfoScreenCaptured(path);
            return path;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log.WarnScreenCaptureFailed(ex, path);
            return null;
        }
    }

    private static byte[] Render(Visual target)
    {
        var scaling = TopLevel.GetTopLevel(target)?.RenderScaling ?? 1d;
        var mode = TextOptions.GetTextRenderingMode(target);
        TextOptions.SetTextRenderingMode(target, TextRenderingMode.Antialias);
        try
        {
            using var bitmap = new RenderTargetBitmap(PixelSize.FromSize(target.Bounds.Size, scaling), new Vector(96 * scaling, 96 * scaling));
            bitmap.Render(target);
            using var stream = new MemoryStream();
            bitmap.Save(stream, PngBitmapEncoderOptions.Default);
            return stream.ToArray();
        }
        finally
        {
            TextOptions.SetTextRenderingMode(target, mode);
        }
    }
}
