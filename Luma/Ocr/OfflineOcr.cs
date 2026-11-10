using System.IO;
using Microsoft.ML.OnnxRuntime;
using RapidOcrNet;
using SkiaSharp;

namespace Luma.Ocr;

internal sealed record OcrWord(string Text, double X, double Y, double Width, double Height, int Line, string SeparatorBefore);

internal static class OfflineOcr
{
    private const int MaxInputSide = 3200;
    private static readonly string ModelFolder = Path.Combine(AppContext.BaseDirectory, "ocr-models");
    private static readonly RapidOcrOptions Options = RapidOcrOptions.PPOCRv6 with
    {
        MaxSideLen = MaxInputSide,
        ReturnWordBox = true
    };

    internal static double InputScale(int width, int height) =>
        Math.Min(1, MaxInputSide / (double)Math.Max(width, height));

    internal static SKBitmap PrepareInput(SKBitmap source)
    {
        double scale = InputScale(source.Width, source.Height);
        var input = new SKBitmap(
            Math.Max(1, (int)Math.Round(source.Width * scale)),
            Math.Max(1, (int)Math.Round(source.Height * scale)),
            SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var canvas = new SKCanvas(input);
        canvas.Clear(SKColors.White);
        source.SetImmutable();
        using var image = SKImage.FromBitmap(source);
        canvas.DrawImage(image, new SKRect(0, 0, input.Width, input.Height),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
        return input;
    }

    internal static IReadOnlyList<OcrWord> Recognize(SKBitmap input, int width, int height,
        string language, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        OrtEnv.Instance().DisableTelemetryEvents();
        var models = Models(language);
        var result = Detect(models, Options);
        if (result.TextBlocks.Length == 0 && Math.Max(input.Width, input.Height) >= 1600)
        {
            // The v5 detector can find small text the v6 detector missed.
            var fallback = RapidOcrModelSet.PPOCRv5Latin;
            var fallbackModels = models with
            {
                DetModelPath = Path.Combine(ModelFolder, Path.GetFileName(fallback.DetModelPath)),
                DetMean = fallback.DetMean,
                DetStd = fallback.DetStd
            };
            var options = RapidOcrOptions.Default with { ImgResize = 2200, ReturnWordBox = true };
            result = Detect(fallbackModels, options);
        }
        var words = new List<OcrWord>();
        double xScale = (double)width / input.Width;
        double yScale = (double)height / input.Height;
        int line = 0;
        foreach (var block in result.TextBlocks)
        {
            token.ThrowIfCancellationRequested();
            if (block.WordResults is { Length: > 0 } wordResults)
            {
                int offset = 0;
                foreach (var word in wordResults)
                {
                    int start = block.Text.IndexOf(word.Text, offset, StringComparison.Ordinal);
                    string separator = start >= offset ? block.Text[offset..start] : " ";
                    AddWord(word.Text, word.BoxPoints, separator);
                    if (start >= offset) offset = start + word.Text.Length;
                }
            }
            else
            {
                AddWord(block.Text, block.BoxPoints, "");
            }

            void AddWord(string text, SKPointI[] box, string separator)
            {
                if (string.IsNullOrWhiteSpace(text) || box.Length == 0) return;
                int left = Math.Clamp(box.Min(point => point.X), 0, input.Width);
                int top = Math.Clamp(box.Min(point => point.Y), 0, input.Height);
                int right = Math.Clamp(box.Max(point => point.X), 0, input.Width);
                int bottom = Math.Clamp(box.Max(point => point.Y), 0, input.Height);
                if (right > left && bottom > top)
                    words.Add(new(text, left * xScale, top * yScale,
                        (right - left) * xScale, (bottom - top) * yScale, line, separator));
            }

            line++;
        }
        return words;

        OcrResult Detect(RapidOcrModelSet modelSet, RapidOcrOptions options)
        {
            token.ThrowIfCancellationRequested();
            using var engine = new RapidOcr();
            engine.InitModels(modelSet, numThread: 2);
            return engine.Detect(input, options, token);
        }
    }

    private static RapidOcrModelSet Models(string language)
    {
        var model = RapidOcrModelSet.PPOCRv6Small;
        string? script = language switch
        {
            "ru" => "cyrillic",
            "ko" => "korean",
            _ => null
        };

        return model with
        {
            DetModelPath = Path.Combine(ModelFolder, Path.GetFileName(model.DetModelPath)),
            ClsModelPath = Path.Combine(ModelFolder, Path.GetFileName(model.ClsModelPath)),
            RecModelPath = Path.Combine(ModelFolder, script == null
                ? Path.GetFileName(model.RecModelPath) : $"{script}_PP-OCRv5_rec_mobile.onnx"),
            KeysPath = Path.Combine(ModelFolder, script == null
                ? Path.GetFileName(model.KeysPath) : $"ppocrv5_{script}_dict.txt")
        };
    }
}
