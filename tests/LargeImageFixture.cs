using System.IO;

// Uncompressed BMP intentionally exceeds both former application limits.
internal static class LargeImageFixture
{
    public static string Create(int width = 9000, int height = 6000)
    {
        string path = Path.Combine(Path.GetTempPath(), "luma-large-" + Guid.NewGuid() + ".bmp");
        int stride = checked((width * 3 + 3) / 4 * 4);
        using var output = new BinaryWriter(File.Create(path));
        output.Write((ushort)0x4D42); output.Write(checked(54 + stride * height));
        output.Write(0); output.Write(54); output.Write(40); output.Write(width); output.Write(height);
        output.Write((ushort)1); output.Write((ushort)24); output.Write(0); output.Write(stride * height);
        output.Write(3780); output.Write(3780); output.Write(0); output.Write(0);
        var row = new byte[stride];
        for (int y=0; y<height; y++) output.Write(row);
        return path;
    }
}
