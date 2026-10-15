using System.Windows.Media.Imaging;

namespace Luma.Imaging;

public sealed record ImageFrame(BitmapSource Bitmap, TimeSpan Delay);

// Frozen BitmapSources cross the decode/UI thread boundary safely.
// No file streams or native images survive decoding.
public sealed record ImageDocument(IReadOnlyList<ImageFrame> Frames, uint Iterations)
{
    public bool Animated => Frames.Count > 1;
}
