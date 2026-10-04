namespace Template.EmbeddedApp.Converters;

using Avalonia.Data.Converters;

using Template.EmbeddedApp.Devices.BuildHat;

public sealed class MotorNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not MotorStatus motor
            ? "-"
            : motor.Link switch
            {
                MotorLink.Disabled => "Disabled",
                MotorLink.Waiting => "Not connected",
                MotorLink.Unsupported => "Unsupported: " + motor.Name,
                _ => motor.Name
            };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
