namespace Template.EmbeddedApp.Devices.Input;

using System;

public interface IInputDevice
{
    event EventHandler<EventArgs<InputSignal>> Handle;
}
