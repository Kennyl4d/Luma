using System.IO;
using System.Runtime.InteropServices;

namespace Luma.Services;

public static class FolderNavigator
{
    public static readonly string[] Extensions = [".jpg", ".jpeg", ".png", ".gif", ".heic", ".heif", ".webp", ".avif", ".bmp", ".dib", ".tif", ".tiff", ".ico", ".svg"];
    public static string DialogFilter => string.Join(';', Extensions.Select(extension => "*" + extension));
    public const string FormatNames = "JPEG · PNG · GIF · HEIC/HEIF · WebP · AVIF · BMP/DIB · TIFF · ICO · SVG";
    public static bool Supported(string path) => Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public static string[] Scan(string path, CancellationToken token)
    {
        var files = new List<string>();
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(path)!))
        {
            token.ThrowIfCancellationRequested();
            if (Supported(file)) files.Add(file);
        }
        files.Sort((a, b) => StrCmpLogicalW(Path.GetFileName(a), Path.GetFileName(b)));
        return files.ToArray();
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string a, string b);
}
