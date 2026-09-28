namespace Template.EmbeddedApp.Devices.Input;

using System;

using Template.EmbeddedApp.State;

public interface IInputDevice
{
    event EventHandler<EventArgs<InputSignal>> Handle;

    DeviceStatus Status { get; }

    bool IsConnected { get; }

    IReadOnlyList<InputKeyBinding> Bindings { get; }

    bool IsPressed(InputKey key);
}
