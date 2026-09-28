namespace Template.EmbeddedApp.Devices.Input;

public class InputButtonOption
{
    public InputKey Key { get; set; }

    public int LongPressMilliseconds { get; set; }

    public int RepeatDelayMilliseconds { get; set; }

    public int RepeatIntervalMilliseconds { get; set; }

    public int MinimumIntervalMilliseconds { get; set; }
}

public sealed class PadButtonOption : InputButtonOption
{
    public byte Button { get; set; }
}

public sealed class PadAxisOption
{
    public byte Axis { get; set; }

    public InputKey Negative { get; set; }

    public InputKey Positive { get; set; }

    public int Threshold { get; set; } = 16384;
}

public sealed class InputProfileOption
{
    public Collection<PadButtonOption> Pad { get; } = [];

    public Collection<PadAxisOption> PadAxes { get; } = [];
}

public sealed class InputOption : IValidatableObject
{
    [Required]
    public string Profile { get; set; } = "Default";

    [Required]
    public string PadDevice { get; set; } = "/dev/input/js0";

    public Dictionary<string, InputProfileOption> Profiles { get; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Profiles.TryGetValue(Profile, out var profile))
        {
            yield return new ValidationResult($"Input profile is not defined. profile=[{Profile}]", [nameof(Profile)]);
            yield break;
        }

        foreach (var button in profile.Pad)
        {
            if (button.Key == InputKey.Unknown)
            {
                yield return new ValidationResult($"Input key is not specified. profile=[{Profile}]", [nameof(Profiles)]);
            }

            if ((button.LongPressMilliseconds > 0) && (button.RepeatDelayMilliseconds > 0))
            {
                yield return new ValidationResult($"Long press and repeat cannot be combined. profile=[{Profile}], key=[{button.Key}]", [nameof(Profiles)]);
            }

            if ((button.RepeatDelayMilliseconds > 0) && (button.RepeatIntervalMilliseconds <= 0))
            {
                yield return new ValidationResult($"Repeat interval is not specified. profile=[{Profile}], key=[{button.Key}]", [nameof(Profiles)]);
            }
        }

        foreach (var axis in profile.PadAxes)
        {
            if ((axis.Negative == InputKey.Unknown) && (axis.Positive == InputKey.Unknown))
            {
                yield return new ValidationResult($"Input key is not specified. profile=[{Profile}], axis=[{axis.Axis}]", [nameof(Profiles)]);
            }

            if (axis.Threshold is < 1 or > Int16.MaxValue)
            {
                yield return new ValidationResult($"Axis threshold is out of range. profile=[{Profile}], axis=[{axis.Axis}]", [nameof(Profiles)]);
            }
        }
    }
}
