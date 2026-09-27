namespace Template.EmbeddedApp.Devices.Input;

using System;

public interface IInputDevice
{
    event EventHandler<EventArgs<InputSignal>> Handle;

    bool IsConnected { get; }

    bool IsPressed(InputKey key);
}
