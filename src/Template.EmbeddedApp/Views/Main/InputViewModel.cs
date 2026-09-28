namespace Template.EmbeddedApp.Views.Main;

using System.Reactive.Concurrency;

using Avalonia.Threading;

using Template.EmbeddedApp.Devices.Input;
using Template.EmbeddedApp.State;

public sealed record InputRecord(DateTimeOffset At, InputKey Key, InputAction Action);

public sealed partial class InputKeyItem : ObservableObject
{
    public InputKey Key { get; }

    public string Source { get; }

    [ObservableProperty]
    public partial bool IsPressed { get; set; }

    [ObservableProperty]
    public partial string LastAction { get; set; } = "-";

    public InputKeyItem(InputKeyBinding binding)
    {
        Key = binding.Key;
        Source = binding.Source;
    }
}

public sealed class InputViewModel : AppViewModelBase
{
    private const int InputHistory = 8;

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(50);

    private static readonly InputKey[] DisplayKeys =
    [
        InputKey.Left,
        InputKey.Right,
        InputKey.Button1,
        InputKey.Button2,
        InputKey.Button3,
        InputKey.Button4
    ];

    private readonly IInputDevice input;

    private readonly DispatcherTimer timer;

    public DeviceStatus Device { get; }

    public string Profile { get; }

    public IReadOnlyList<InputKeyItem> Keys { get; }

    public ObservableCollection<InputRecord> Inputs { get; } = [];

    public InputViewModel(TimeProvider timeProvider, InputOption option, IInputDevice input)
    {
        this.input = input;
        Device = input.Status;
        Profile = option.Profile;
        Keys = [.. DisplayKeys.SelectMany(key => input.Bindings.Where(x => x.Key == key)).Select(static x => new InputKeyItem(x))];

        var scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
        Disposables.Add(Observable
            .FromEvent<EventHandler<EventArgs<InputSignal>>, EventArgs<InputSignal>>(static h => (_, e) => h(e), h => input.Handle += h, h => input.Handle -= h)
            .Where(static x => DisplayKeys.Contains(x.Data.Key))
            .Select(x => new InputRecord(timeProvider.GetLocalNow(), x.Data.Key, x.Data.Action))
            .ObserveOn(scheduler)
            .Subscribe(AddInput));

        timer = new DispatcherTimer { Interval = RefreshInterval };
        timer.Tick += (_, _) => RefreshPressed();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Stop();
        }

        base.Dispose(disposing);
    }

    public override void OnNavigatedTo(INavigationContext context)
    {
        RefreshPressed();
        timer.Start();
    }

    public override void OnNavigatingFrom(INavigationContext context)
    {
        timer.Stop();
    }

    private void RefreshPressed()
    {
        foreach (var key in Keys)
        {
            key.IsPressed = input.IsPressed(key.Key);
        }
    }

    private void AddInput(InputRecord record)
    {
        Inputs.Insert(0, record);
        if (Inputs.Count > InputHistory)
        {
            Inputs.RemoveAt(Inputs.Count - 1);
        }

        foreach (var key in Keys.Where(x => x.Key == record.Key))
        {
            key.LastAction = record.Action.ToString();
        }
    }
}
