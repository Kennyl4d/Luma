using System.Text;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;

namespace Luma.Ocr;

public sealed class TextSelectionOverlay : Avalonia.Controls.Control
{
    private RecognizedText? text;
    private int start = -1, end = -1;
    private static readonly IBrush highlight = new SolidColorBrush(Color.FromArgb(100, 58, 140, 245));
    public bool HasText => text?.Words.Count > 0;
    public bool HasSelection => start >= 0 && end >= 0;

    public TextSelectionOverlay()
    {
        SizeChanged += (_, _) => InvalidateVisual();
        PointerMoved += (_, e) => Cursor = HitWord(e.GetPosition(this)) >= 0 ? new Cursor(StandardCursorType.Ibeam) : null;
        PointerExited += (_, _) => Cursor = null;
    }
    public void SetText(RecognizedText? value) { text = value; ClearSelection(); }
    public void ClearSelection() { start = end = -1; InvalidateVisual(); }
    public void SelectFrom(int index) { start = end = index; InvalidateVisual(); }
    public void ExtendTo(Point position)
    {
        if (text == null || start < 0) return;
        int index = HitWord(position);
        if (index < 0 && text.Words.Count > 0)
        {
            Point pixel = ToPixel(position);
            index = Enumerable.Range(0, text.Words.Count).MinBy(i => Distance(pixel, text.Words[i].Bounds));
        }
        end = index; InvalidateVisual();
    }
    public int HitWord(Point position)
    {
        if (text == null || Bounds.Width <= 0 || Bounds.Height <= 0) return -1;
        Point pixel = ToPixel(position);
        for (int i = 0; i < text.Words.Count; i++)
            if (text.Words[i].Bounds.Contains(pixel)) return i;
        return -1;
    }
    private Point ToPixel(Point point) => new(point.X * text!.Width / Math.Max(1, Bounds.Width), point.Y * text.Height / Math.Max(1, Bounds.Height));
    private static double Distance(Point point, Rect rect)
    {
        double x = Math.Max(rect.Left - point.X, Math.Max(0, point.X - rect.Right));
        double y = Math.Max(rect.Top - point.Y, Math.Max(0, point.Y - rect.Bottom));
        return x * x + y * y;
    }
    public string SelectedText(bool all = false)
    {
        if (text == null || (!all && !HasSelection)) return "";
        int first = all ? 0 : Math.Min(start, end), last = all ? text.Words.Count - 1 : Math.Max(start, end);
        var result = new StringBuilder(); int previousLine = -1;
        for (int i = first; i <= last; i++)
        {
            var word = text.Words[i];
            if (result.Length > 0)
            {
                bool join = word.Line == previousLine && IsCjk(result[^1]) && IsCjk(word.Text[0]);
                result.Append(word.Line != previousLine ? Environment.NewLine : join ? "" : " ");
            }
            result.Append(word.Text); previousLine = word.Line;
        }
        return result.ToString();
    }
    private static bool IsCjk(char value) => value is >= '\u3000' and <= '\u9FFF' or >= '\uF900' and <= '\uFAFF';
    public override void Render(DrawingContext drawing)
    {
        drawing.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
        if (text == null || !HasSelection) return;
        for (int i = Math.Min(start, end); i <= Math.Max(start, end); i++)
        {
            Rect rect = text.Words[i].Bounds;
            rect = new Rect(rect.X * Bounds.Width / text.Width, rect.Y * Bounds.Height / text.Height, rect.Width * Bounds.Width / text.Width, rect.Height * Bounds.Height / text.Height);
            drawing.DrawRectangle(highlight, null, rect, 2, 2);
        }
    }
}

