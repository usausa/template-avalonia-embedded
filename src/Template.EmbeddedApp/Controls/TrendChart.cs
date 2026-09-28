namespace Template.EmbeddedApp.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

public sealed class TrendChart : Control
{
    private const int GridDivisions = 4;

    public static readonly StyledProperty<IReadOnlyList<double>?> ValuesProperty = AvaloniaProperty.Register<TrendChart, IReadOnlyList<double>?>(nameof(Values));

    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<TrendChart, double>(nameof(Minimum));

    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<TrendChart, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<int> CapacityProperty = AvaloniaProperty.Register<TrendChart, int>(nameof(Capacity), 60);

    public static readonly StyledProperty<IBrush?> StrokeProperty = AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(Stroke), Brushes.DodgerBlue);

    public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<TrendChart, double>(nameof(StrokeThickness), 2);

    public static readonly StyledProperty<IBrush?> GridBrushProperty = AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(GridBrush));

    public IReadOnlyList<double>? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
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

    public int Capacity
    {
        get => GetValue(CapacityProperty);
        set => SetValue(CapacityProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public IBrush? GridBrush
    {
        get => GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    static TrendChart()
    {
        AffectsRender<TrendChart>(ValuesProperty, MinimumProperty, MaximumProperty, CapacityProperty, StrokeProperty, StrokeThicknessProperty, GridBrushProperty);
    }

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if ((width <= 0) || (height <= 0))
        {
            return;
        }

        if (GridBrush is { } gridBrush)
        {
            var gridPen = new Pen(gridBrush);
            for (var i = 0; i <= GridDivisions; i++)
            {
                var y = Math.Round(height * i / GridDivisions);
                context.DrawLine(gridPen, new Point(0, y), new Point(width, y));
            }
        }

        if ((Values is not { Count: > 1 } values) || (Stroke is not { } stroke) || (Maximum <= Minimum))
        {
            return;
        }

        var capacity = Math.Max(Capacity, values.Count);
        var step = width / (capacity - 1);
        var offset = capacity - values.Count;
        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            for (var i = 0; i < values.Count; i++)
            {
                var ratio = Math.Clamp((values[i] - Minimum) / (Maximum - Minimum), 0, 1);
                var point = new Point((offset + i) * step, height - (ratio * height));
                if (i == 0)
                {
                    stream.BeginFigure(point, false);
                }
                else
                {
                    stream.LineTo(point);
                }
            }

            stream.EndFigure(false);
        }

        context.DrawGeometry(null, new Pen(stroke, StrokeThickness, lineJoin: PenLineJoin.Round), geometry);
    }
}
