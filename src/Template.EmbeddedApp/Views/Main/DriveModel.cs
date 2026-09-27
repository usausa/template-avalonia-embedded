namespace Template.EmbeddedApp.Views.Main;

public sealed class DriveModel
{
    private const double MaxModelSpeed = 100;

    private const double Acceleration = 50;

    private const double BrakeDeceleration = 100;

    private const double CoastDeceleration = 25;

    private double speed;

    public void Update(bool accel, bool brake, double seconds)
    {
        if (brake)
        {
            speed = Math.Max(0, speed - (BrakeDeceleration * seconds));
        }
        else if (accel)
        {
            speed = Math.Min(MaxModelSpeed, speed + (Acceleration * seconds));
        }
        else
        {
            speed = Math.Max(0, speed - (CoastDeceleration * seconds));
        }
    }

    public void Reset() => speed = 0;

    public int ToPercent(int maxPercent) => (int)Math.Round(speed * maxPercent / MaxModelSpeed);
}
