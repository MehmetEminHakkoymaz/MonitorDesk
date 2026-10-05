using System.Windows;
using System.Windows.Media;

namespace MonitorDesk;

// A vector sun with a preset-level fill, independent of the monitor's current value.
internal sealed class LightingIcon(double fill) : FrameworkElement
{
    internal static readonly DependencyProperty InkProperty = DependencyProperty.Register(
        nameof(Ink), typeof(Brush), typeof(LightingIcon), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush Ink { get => (Brush)GetValue(InkProperty); set => SetValue(InkProperty, value); }

    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var pen = new Pen(Ink, 1.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        for (int ray = 0; ray < 8; ray++)
        {
            double angle = ray * Math.PI / 4;
            drawing.DrawLine(pen, new(center.X + Math.Cos(angle) * 12, center.Y + Math.Sin(angle) * 12),
                new(center.X + Math.Cos(angle) * 16, center.Y + Math.Sin(angle) * 16));
        }
        drawing.PushClip(new EllipseGeometry(center, 8, 8));
        drawing.DrawRectangle(Ink, null, new Rect(center.X - 8, center.Y + 8 - 16 * Math.Clamp(fill, 0, 1), 16, 16));
        drawing.Pop();
        drawing.DrawEllipse(null, pen, center, 8, 8);
    }
}
