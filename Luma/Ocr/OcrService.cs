using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tesseract;
using Rect = System.Windows.Rect;

namespace Luma.Ocr;

public sealed record RecognizedWord(string Text, Rect Bounds, int Line);
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

    // Native OCR stays off the startup and image decoding paths.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static RecognizedText Recognize(BitmapSource image, string language, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        double scale = Math.Min(1, 2200d / Math.Max(image.PixelWidth, image.PixelHeight));
        BitmapSource input = scale < 1 ? new TransformedBitmap(image, new ScaleTransform(scale, scale)) : image;
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(input));
        using var encoded = new MemoryStream(); encoder.Save(encoded);
        token.ThrowIfCancellationRequested();
        string model = language switch {
            "cs" => "ces", "de" => "deu", "es" => "spa", "fr" => "fra", "it" => "ita",
            "ja" => "jpn", "ko" => "kor", "pl" => "pol", "pt-BR" => "por", "ru" => "rus",
            "tr" => "tur", "zh-Hans" => "chi_sim", "zh-Hant" => "chi_tra", _ => "eng" };
        string models = model == "eng" ? model : model + "+eng";
        if (Environment.GetEnvironmentVariable("OMP_THREAD_LIMIT") == null) Environment.SetEnvironmentVariable("OMP_THREAD_LIMIT", "2");
        using var engine = new TesseractEngine(Path.Combine(AppContext.BaseDirectory, "tessdata"), models, EngineMode.LstmOnly);
        engine.SetVariable("user_defined_dpi", "150");
        using var pixels = Pix.LoadFromMemory(encoded.ToArray());
        using var page = engine.Process(pixels, PageSegMode.SparseText);
        token.ThrowIfCancellationRequested();
        var words = new List<RecognizedWord>();
        using var iterator = page.GetIterator(); iterator.Begin();
        int line = -1;
        do
        {
            token.ThrowIfCancellationRequested();
            if (iterator.IsAtBeginningOf(PageIteratorLevel.TextLine)) line++;
            string? text = iterator.GetText(PageIteratorLevel.Word)?.Trim();
            if (string.IsNullOrWhiteSpace(text) || iterator.GetConfidence(PageIteratorLevel.Word) < 35 ||
                !iterator.TryGetBoundingBox(PageIteratorLevel.Word, out var box)) continue;
            double xScale = (double)image.PixelWidth / input.PixelWidth;
            double yScale = (double)image.PixelHeight / input.PixelHeight;
            words.Add(new(text, new Rect(box.X1 * xScale, box.Y1 * yScale, box.Width * xScale, box.Height * yScale), line));
        } while (iterator.Next(PageIteratorLevel.Word));
        return new(image.PixelWidth, image.PixelHeight, words);
    }
}
