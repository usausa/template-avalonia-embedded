namespace Template.EmbeddedApp.Devices.BuildHat;

using System.Diagnostics;
using System.IO.Ports;

using Iot.Device.BuildHat;

public sealed class BuildHatConnection : IDisposable
{
    private const string FirmwarePrefix = "Firmware version: ";

    private const string BootloaderPrefix = "BuildHAT bootloader version";

    private const string InitialisedMessage = "Done initialising ports";

    private const int PortCount = 4;

    private const int VersionRetry = 5;

    private const int BlockSize = 1024;

    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan PromptTimeout = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan InitialiseTimeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan LoadWait = TimeSpan.FromMilliseconds(100);

    private readonly SerialPort port;

    public string Firmware { get; private set; } = string.Empty;

    public bool FirmwareLoaded { get; private set; }

    private BuildHatConnection(SerialPort port)
    {
        this.port = port;
    }

    public static BuildHatConnection Open(string device, CancellationToken token)
    {
        var connection = new BuildHatConnection(new SerialPort(device, 115200, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 500,
            WriteTimeout = 5000,
            NewLine = "\r\n"
        });
        try
        {
            connection.Initialize(token);
            var opened = connection;
            connection = null;
            return opened;
        }
        finally
        {
            connection?.Dispose();
        }
    }

    public void Dispose()
    {
        if (port.IsOpen)
        {
            try
            {
                port.BaseStream.Flush();
            }
            catch (IOException)
            {
            }
        }

        port.Dispose();
    }

    public void Send(string command) => port.Write(command + "\r");

    public string? ReadLine()
    {
        try
        {
            return port.ReadLine();
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    private void Initialize(CancellationToken token)
    {
        port.Open();
        port.DiscardInBuffer();

        var version = ReadVersion(token);
        if (version.Contains(BootloaderPrefix, StringComparison.Ordinal))
        {
            LoadFirmware(token);
            version = ReadVersion(token);
            FirmwareLoaded = true;
        }

        if (!version.StartsWith(FirmwarePrefix, StringComparison.Ordinal))
        {
            throw new IOException("Build HAT firmware is not running.");
        }

        Firmware = version[FirmwarePrefix.Length..];
        Send("echo 0");
        for (var i = 0; i < PortCount; i++)
        {
            Send(String.Create(CultureInfo.InvariantCulture, $"port {i} ; select ; pwm ; coast"));
        }
    }

    private string ReadVersion(CancellationToken token)
    {
        for (var i = 0; i < VersionRetry; i++)
        {
            Send("version");
            if (WaitForLine(static x => x.StartsWith(FirmwarePrefix, StringComparison.Ordinal) || x.Contains(BootloaderPrefix, StringComparison.Ordinal), VersionTimeout, token) is { } line)
            {
                return line;
            }
        }

        throw new IOException("Build HAT does not respond.");
    }

    private string? WaitForLine(Func<string, bool> predicate, TimeSpan timeout, CancellationToken token)
    {
        var start = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(start) < timeout)
        {
            token.ThrowIfCancellationRequested();
            if ((ReadLine() is { } line) && predicate(line))
            {
                return line;
            }
        }

        return null;
    }

    private void LoadFirmware(CancellationToken token)
    {
        var firmware = ReadResource("firmware.bin");
        var signature = ReadResource("signature.bin");

        Send("clear");
        WaitForPrompt(token);
        Send(String.Create(CultureInfo.InvariantCulture, $"load {firmware.Length} {Checksum(firmware)}"));
        token.WaitHandle.WaitOne(LoadWait);
        WriteBlock(firmware);
        WaitForPrompt(token);
        Send(String.Create(CultureInfo.InvariantCulture, $"signature {signature.Length}"));
        token.WaitHandle.WaitOne(LoadWait);
        WriteBlock(signature);
        WaitForPrompt(token);
        Send("reboot");
        if (WaitForLine(static x => x.Contains(InitialisedMessage, StringComparison.Ordinal), InitialiseTimeout, token) is null)
        {
            throw new IOException("Build HAT does not finish initialising.");
        }
    }

    private void WaitForPrompt(CancellationToken token)
    {
        var start = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(start) < PromptTimeout)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                if (port.ReadChar() == '>')
                {
                    return;
                }
            }
            catch (TimeoutException)
            {
            }
        }

        throw new IOException("Build HAT bootloader does not respond.");
    }

    private void WriteBlock(byte[] data)
    {
        port.Write([0x02], 0, 1);
        for (var offset = 0; offset < data.Length; offset += BlockSize)
        {
            port.Write(data, offset, Math.Min(BlockSize, data.Length - offset));
        }

        port.Write([0x03, 0x0D], 0, 2);
    }

    private static byte[] ReadResource(string name)
    {
        using var stream = typeof(Brick).Assembly.GetManifestResourceStream("Iot.Device.Bindings." + name) ??
                           throw new InvalidOperationException($"Build HAT resource is not found. name=[{name}]");
        var buffer = new byte[stream.Length];
        stream.ReadExactly(buffer);
        return buffer;
    }

    private static uint Checksum(ReadOnlySpan<byte> data)
    {
        var sum = 1u;
        foreach (var value in data)
        {
            sum = (sum & 0x80000000u) != 0 ? (sum << 1) ^ 0x1D872B41u : sum << 1;
            sum ^= value;
        }

        return sum;
    }
}
