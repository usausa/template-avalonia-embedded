namespace Template.EmbeddedApp.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

public sealed class ArcGauge : Control
{
    private const double StartAngle = 135;

    private const double SweepAngle = 270;

    public static readonly StyledProperty<double> LevelProperty = AvaloniaProperty.Register<ArcGauge, double>(nameof(Level));

    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<ArcGauge, double>(nameof(Minimum));

    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<ArcGauge, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<double> ThicknessProperty = AvaloniaProperty.Register<ArcGauge, double>(nameof(Thickness), 16);

    public static readonly StyledProperty<IBrush?> TrackBrushProperty = AvaloniaProperty.Register<ArcGauge, IBrush?>(nameof(TrackBrush), Brushes.LightGray);

    public static readonly StyledProperty<IBrush?> ValueBrushProperty = AvaloniaProperty.Register<ArcGauge, IBrush?>(nameof(ValueBrush), Brushes.DodgerBlue);

    public double Level
    {
        get => GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Thickness
    {
        get => GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush? ValueBrush
    {
        get => GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    static ArcGauge()
    {
        AffectsRender<ArcGauge>(LevelProperty, MinimumProperty, MaximumProperty, ThicknessProperty, TrackBrushProperty, ValueBrushProperty);
    }

    public override void Render(DrawingContext context)
    {
        var thickness = Thickness;
        var radius = (Math.Min(Bounds.Width, Bounds.Height) - thickness) / 2;
        if (radius <= 0)
        {
            return;
        }

        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var ratio = Maximum > Minimum ? Math.Clamp((Level - Minimum) / (Maximum - Minimum), 0, 1) : 0;

        DrawArc(context, TrackBrush, center, radius, thickness, SweepAngle);
        if (ratio > 0)
        {
            DrawArc(context, ValueBrush, center, radius, thickness, SweepAngle * ratio);
        }
    }

    private static void DrawArc(DrawingContext context, IBrush? brush, Point center, double radius, double thickness, double sweep)
    {
        if (brush is null)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(GetPoint(center, radius, StartAngle), false);
            stream.ArcTo(GetPoint(center, radius, StartAngle + sweep), new Size(radius, radius), 0, sweep > 180, SweepDirection.Clockwise);
            stream.EndFigure(false);
        }

        context.DrawGeometry(null, new Pen(brush, thickness, lineCap: PenLineCap.Round), geometry);
    }

    private static Point GetPoint(Point center, double radius, double angle)
    {
        var radian = angle * Math.PI / 180;
        return new Point(center.X + (radius * Math.Cos(radian)), center.Y + (radius * Math.Sin(radian)));
    }
}
