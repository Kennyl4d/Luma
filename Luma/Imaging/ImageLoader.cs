using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageMagick;
using Luma.Services;

namespace Luma.Imaging;

public sealed class ImageLoader
{
    private readonly SemaphoreSlim gate = new(1);
    private const long MaxPixels = 40_000_000;
    private const long MaxAnimationPixels = 24_000_000;
    private static readonly Lazy<string> nativeConfiguration = new(InitializeNative);

    private static string InitializeNative()
    {
        // MSVG creates an internal MVG drawing. The bundled configuration allows that read operation.
        // It preserves the upstream restrictions and disables external delegates and network coders.
        string directory = Path.Combine(AppContext.BaseDirectory, "DecoderConfig");
        MagickNET.Initialize(directory);
        return directory;
    }

    public async Task<ImageDocument> LoadAsync(string path, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            return await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                if (new FileInfo(path).Length > 128 * 1024 * 1024)
                    throw new InvalidDataException(L.Text("FileTooLarge"));
                var extension = DetectFormat(path);
                var result = extension switch
                {
                    ".jpg" or ".jpeg" or ".png" or ".bmp" => LoadWindows(path),
                    ".gif" or ".heic" or ".heif" or ".webp" or ".avif" or ".dib" or ".tif" or ".tiff" or ".ico" or ".svg" => LoadNative(path, extension, token),
                    _ => throw new InvalidDataException(L.Text("UnsupportedFormat"))
                };
                token.ThrowIfCancellationRequested();
                return result;
            }, token);
        }
        finally { gate.Release(); }
    }

    // Some exported/downloaded photos retain .HEIC even after conversion to JPEG.
    // Sniff only the supported raster signatures; retain the existing HEIF path otherwise.
    private static string DetectFormat(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[8];
        int count = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
        if (count >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return ".jpg";
        if (count == 8 && header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return ".png";
        if (count >= 6 && (header[..6].SequenceEqual("GIF87a"u8) || header[..6].SequenceEqual("GIF89a"u8)))
            return ".gif";
        if (count >= 2 && header[0] == 'B' && header[1] == 'M') return ".bmp";
        return Path.GetExtension(path).ToLowerInvariant();
    }

    private static ImageDocument LoadWindows(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
        var frame = decoder.Frames[0];
        CheckSize(frame.PixelWidth, frame.PixelHeight);
        int orientation = 1;
        if (frame.Metadata is BitmapMetadata metadata)
        {
            try { if (metadata.GetQuery("/app1/ifd/{ushort=274}") is ushort value) orientation = value; }
            catch (NotSupportedException) { }
        }
        // OnLoad detaches the decoder from the file without a full managed pixel-array copy.
        stream.Position = 0;
        var cached = new BitmapImage();
        cached.BeginInit();
        cached.CacheOption = BitmapCacheOption.OnLoad;
        cached.StreamSource = stream;
        cached.EndInit();
        BitmapSource bitmap = cached;
        Transform? transform = orientation switch
        {
            2 => new ScaleTransform(-1, 1), 3 => new RotateTransform(180),
            4 => new ScaleTransform(1, -1),
            5 => Combine(new ScaleTransform(-1, 1), new RotateTransform(270)),
            6 => new RotateTransform(90),
            7 => Combine(new ScaleTransform(-1, 1), new RotateTransform(90)),
            8 => new RotateTransform(270), _ => null
        };
        if (transform != null) bitmap = new TransformedBitmap(bitmap, transform);
        bitmap.Freeze();
        return new([new(bitmap, TimeSpan.Zero)], 1);
    }

    private static Transform Combine(Transform a, Transform b)
    {
        var group = new TransformGroup(); group.Children.Add(a); group.Children.Add(b); return group;
    }

    private static void CheckSize(int width, int height)
    {
        if (width <= 0 || height <= 0 || (long)width * height > MaxPixels)
            throw new InvalidDataException(L.Text("ImageTooLarge"));
    }

    // Keep the native library outside the JPEG/PNG startup path.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ImageDocument LoadNative(string path, string extension, CancellationToken token)
    {
        _ = nativeConfiguration.Value;
        ResourceLimits.Memory = 192UL * 1024 * 1024;
        ResourceLimits.MaxMemoryRequest = 192UL * 1024 * 1024;
        ResourceLimits.ListLength = 1000;
        ResourceLimits.Disk = 0;
        ResourceLimits.Thread = 2;
        bool animated = extension == ".gif";
        var settings = new MagickReadSettings { Format = extension switch {
            ".gif" => MagickFormat.Gif, ".webp" => MagickFormat.WebP, ".avif" => MagickFormat.Avif,
            ".dib" => MagickFormat.Dib, ".tif" or ".tiff" => MagickFormat.Tiff,
            ".ico" => MagickFormat.Ico, ".svg" => MagickFormat.Msvg, _ => MagickFormat.Heic } };
        if (!animated && extension != ".ico") { settings.FrameIndex = 0; settings.FrameCount = 1; }
        // SVG is parsed without DTDs and external resources, then rasterized by the in-process renderer.
        byte[]? svg = extension == ".svg" ? SvgInput.Read(path) : null;
        using var images = new MagickImageCollection();
        if (svg == null) images.Ping(path, settings); else images.Ping(svg, settings);
        if (images.Count == 0) throw new InvalidDataException(L.Text("NoImages"));
        if (extension == ".ico")
        {
            int largest = Enumerable.Range(0, images.Count).MaxBy(i => (long)images[i].Width * images[i].Height);
            settings.FrameIndex = (uint)largest; settings.FrameCount = 1;
        }
        long canvasWidth = 0, canvasHeight = 0;
        foreach (var image in images)
        {
            CheckSize(checked((int)image.Width), checked((int)image.Height));
            long width = Math.Max(image.Width, image.Page.Width);
            long height = Math.Max(image.Height, image.Page.Height);
            canvasWidth = Math.Max(canvasWidth, width);
            canvasHeight = Math.Max(canvasHeight, height);
        }
        long animationPixels = checked(canvasWidth * canvasHeight * images.Count);
        if (animated && (images.Count > 1000 || animationPixels > MaxAnimationPixels))
            throw new InvalidDataException(L.Text("GifTooLarge"));
        token.ThrowIfCancellationRequested();
        if (svg == null) images.Read(path, settings); else images.Read(svg, settings);
        uint iterations = images[0].AnimationIterations;
        if (animated) images.Coalesce(); // Applies offsets, transparency and disposal 2/3.
        else images[0].AutoOrient();
        var frames = new List<ImageFrame>(images.Count);
        foreach (var image in images)
        {
            token.ThrowIfCancellationRequested();
            CheckSize(checked((int)image.Width), checked((int)image.Height));
            using var pixels = image.GetPixels();
            byte[] bytes = pixels.ToByteArray(PixelMapping.BGRA) ?? throw new InvalidDataException(L.Text("PixelsUnreadable"));
            var bitmap = BitmapSource.Create((int)image.Width, (int)image.Height, 96, 96,
                PixelFormats.Bgra32, null, bytes, checked((int)image.Width * 4));
            bitmap.Freeze();
            double ticks = image.AnimationTicksPerSecond == 0 ? 100 : image.AnimationTicksPerSecond;
            double milliseconds = image.AnimationDelay * 1000d / ticks;
            // Browsers commonly normalize zero/very short delays; retain valid >=20ms delays.
            frames.Add(new(bitmap, TimeSpan.FromMilliseconds(milliseconds < 20 ? 100 : milliseconds)));
        }
        return new(frames, iterations);
    }
}
