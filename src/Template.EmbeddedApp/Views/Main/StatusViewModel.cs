namespace Template.EmbeddedApp.Views.Main;

using System.Reactive.Concurrency;

using Template.EmbeddedApp.Devices.Input;
using Template.EmbeddedApp.State;

public sealed record InputRecord(DateTimeOffset At, InputKey Key, InputAction Action);

public sealed class StatusViewModel : AppViewModelBase
{
    private const int InputHistory = 8;

    public ObservableCollection<DeviceStatus> Devices { get; }

    public ObservableCollection<InputRecord> Inputs { get; } = [];

    public StatusViewModel(TimeProvider timeProvider, DeviceState deviceState, IInputDevice input)
    {
        Devices = deviceState.Devices;

        var scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!);
        Disposables.Add(Observable
            .FromEvent<EventHandler<EventArgs<InputSignal>>, EventArgs<InputSignal>>(static h => (_, e) => h(e), h => input.Handle += h, h => input.Handle -= h)
            .Select(x => new InputRecord(timeProvider.GetLocalNow(), x.Data.Key, x.Data.Action))
            .ObserveOn(scheduler)
            .Subscribe(AddInput));
    }

    protected override async ValueTask OnNavigationBackAsync()
    {
        await Navigator.PopAsync();
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
