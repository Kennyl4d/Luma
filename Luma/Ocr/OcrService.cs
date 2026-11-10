using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;

namespace Luma.Ocr;

public sealed record RecognizedWord(string Text, Rect Bounds, int Line, string SeparatorBefore = " ");
public sealed record RecognizedText(int Width, int Height, IReadOnlyList<RecognizedWord> Words);

public sealed class OcrService
{
    private readonly SemaphoreSlim gate = new(1);

    public async Task<RecognizedText> RecognizeAsync(BitmapSource image, string language, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try { return await Task.Run(() => Recognize(image, language, token), token); }
        finally { gate.Release(); }
    }

    private static RecognizedText Recognize(BitmapSource image, string language, CancellationToken token)
    {
        int width = image.PixelWidth;
        int height = image.PixelHeight;
        double scale = OfflineOcr.InputScale(width, height);
        BitmapSource input = scale < 1 ? new TransformedBitmap(image, new ScaleTransform(scale, scale)) : image;
        if (input.Format != PixelFormats.Bgra32)
            input = new FormatConvertedBitmap(input, PixelFormats.Bgra32, null, 0);

        token.ThrowIfCancellationRequested();
        using var bitmap = new SKBitmap(input.PixelWidth, input.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        input.CopyPixels(Int32Rect.Empty, bitmap.GetPixels(), bitmap.ByteCount, bitmap.RowBytes);

        using var prepared = OfflineOcr.PrepareInput(bitmap);
        var words = OfflineOcr.Recognize(prepared, width, height, language, token);
        return new(width, height, words.Select(word => new RecognizedWord(word.Text,
            new Rect(word.X, word.Y, word.Width, word.Height), word.Line, word.SeparatorBefore)).ToArray());
    }
}
