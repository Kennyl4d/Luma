using Avalonia;
using Luma.Imaging;
using SkiaSharp;

namespace Luma.Ocr;

public sealed record RecognizedWord(string Text, Rect Bounds, int Line, string SeparatorBefore = " ");
public sealed record RecognizedText(int Width, int Height, IReadOnlyList<RecognizedWord> Words);

public sealed class OcrService
{
    private readonly SemaphoreSlim gate = new(1);

    public async Task<RecognizedText> RecognizeAsync(ImageFrame frame, string language, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try { return await Task.Run(() => Recognize(frame, language, token), token); }
        finally { gate.Release(); }
    }

    private static RecognizedText Recognize(ImageFrame frame, string language, CancellationToken token)
    {
        int width = frame.Bitmap.PixelSize.Width;
        int height = frame.Bitmap.PixelSize.Height;
        var pixels = frame.Pixels ?? throw new InvalidOperationException("OCR requires a decoded still image.");
        using var input = PrepareInput(width, height, pixels);
        var words = OfflineOcr.Recognize(input, width, height, language, token);
        return new(width, height, words.Select(word => new RecognizedWord(word.Text,
            new Rect(word.X, word.Y, word.Width, word.Height), word.Line, word.SeparatorBefore)).ToArray());
    }

    private static unsafe SKBitmap PrepareInput(int width, int height, byte[] pixels)
    {
        fixed (byte* data = pixels)
        {
            using var bitmap = new SKBitmap();
            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            if (!bitmap.InstallPixels(info, (IntPtr)data, info.RowBytes))
                throw new InvalidOperationException("Unable to read image pixels for OCR.");
            return OfflineOcr.PrepareInput(bitmap);
        }
    }
}
