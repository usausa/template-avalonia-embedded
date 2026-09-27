namespace Template.EmbeddedApp.Devices.BuildHat;

public sealed class DriveOption
{
    public BuildHatPort Port { get; set; } = BuildHatPort.A;

    [Range(1, 100)]
    public int MaxSpeed { get; set; } = 60;

    public bool Reverse { get; set; }
}

public sealed class SteeringOption
{
    public BuildHatPort Port { get; set; } = BuildHatPort.B;

    [Range(-180, 179)]
    public int Center { get; set; }

    [Range(1, 90)]
    public int MaxAngle { get; set; } = 30;

    [Range(1, 100)]
    public int Speed { get; set; } = 50;

    public bool Reverse { get; set; }
}

public sealed class BuildHatOption : IValidatableObject
{
    public string Device { get; set; } = string.Empty;

    public DriveOption Drive { get; } = new();

    public SteeringOption Steering { get; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(Drive, new ValidationContext(Drive), results, true);
        Validator.TryValidateObject(Steering, new ValidationContext(Steering), results, true);
        if (Drive.Port == Steering.Port)
        {
            results.Add(new ValidationResult($"Drive and steering use the same port. port=[{Drive.Port}]", [nameof(Drive), nameof(Steering)]));
        }

        return results;
    }
}
