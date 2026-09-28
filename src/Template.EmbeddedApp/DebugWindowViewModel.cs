namespace Template.EmbeddedApp;

using System.Reactive.Concurrency;

using Avalonia.Input;

using Smart.Avalonia.ViewModels;

using Template.EmbeddedApp.Devices.Input;
using Template.EmbeddedApp.Settings;
using Template.EmbeddedApp.State;
using Template.EmbeddedApp.Views.Main;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public class DebugWindowViewModel : ExtendViewModelBase
{
    private const int InputHistory = 12;

    private readonly DebugInputDevice input;

    public int ScreenWidth { get; }

    public int ScreenHeight { get; }

    public IReadOnlyList<DebugKey> Keys { get; }

    public ObservableCollection<DeviceStatus> Devices { get; }

    public ObservableCollection<InputRecord> Inputs { get; } = [];

    public ICommand ConnectCommand { get; }

    public DebugWindowViewModel(TimeProvider timeProvider, DisplaySetting display, DeviceState deviceState, DebugInputDevice input)
    {
        this.input = input;
        ScreenWidth = display.Width;
        ScreenHeight = display.Height;
        Keys = [.. input.Keys.Select(static (x, i) => new DebugKey(x, i + 1))];
        Devices = deviceState.Devices;
        ConnectCommand = MakeDelegateCommand<bool?>(x => Connect(x == true));

        var scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
        Disposables.Add(Observable
            .FromEvent<EventHandler<EventArgs<InputSignal>>, EventArgs<InputSignal>>(static h => (_, e) => h(e), h => input.Handle += h, h => input.Handle -= h)
            .Select(x => new InputRecord(timeProvider.GetLocalNow(), x.Data.Key, x.Data.Action))
            .ObserveOn(scheduler)
            .Subscribe(AddInput));
    }

    public DebugKey? FindKey(Key key) => key switch
    {
        >= Key.D1 and <= Key.D9 => key - Key.D1 < Keys.Count ? Keys[key - Key.D1] : null,
        Key.Left => Keys.FirstOrDefault(static x => x.Key == InputKey.Left),
        Key.Right => Keys.FirstOrDefault(static x => x.Key == InputKey.Right),
        _ => null
    };

    public void Press(DebugKey key)
    {
        if (key.IsPressed || !input.IsConnected)
        {
            return;
        }

        key.IsPressed = true;
        input.Press(key.Key);
    }

    public void Release(DebugKey key)
    {
        if (!key.IsPressed)
        {
            return;
        }

        key.IsPressed = false;
        input.Release(key.Key);
    }

    public void ReleaseAll()
    {
        foreach (var key in Keys)
        {
            Release(key);
        }
    }

    private void Connect(bool value)
    {
        if (!value)
        {
            ReleaseAll();
        }

        input.SetConnected(value);
    }

    private void AddInput(InputRecord record)
    {
        Inputs.Insert(0, record);
        if (Inputs.Count > InputHistory)
        {
            Inputs.RemoveAt(Inputs.Count - 1);
        }
    }
}
