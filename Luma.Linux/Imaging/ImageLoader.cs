using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;
using System.Runtime.InteropServices;
using Luma.Services;

namespace Luma.Imaging;
public sealed record ImageFrame(Bitmap Bitmap, TimeSpan Delay, byte[]? Pixels);
public sealed record ImageDocument(IReadOnlyList<ImageFrame> Frames, uint Iterations) : IDisposable
{
 public bool Animated => Frames.Count > 1;
 public void Dispose() { foreach (var frame in Frames) frame.Bitmap.Dispose(); }
}
public sealed class ImageLoader
{
 private readonly SemaphoreSlim gate=new(1);
 private static readonly Lazy<bool> configuration = new(() => { MagickNET.Initialize(Path.Combine(AppContext.BaseDirectory,"DecoderConfig")); ResourceLimits.Memory=192UL*1024*1024; ResourceLimits.MaxMemoryRequest=192UL*1024*1024; ResourceLimits.ListLength=1000; ResourceLimits.Disk=0; ResourceLimits.Thread=2; return true; });
 public async Task<ImageDocument> LoadAsync(string path,CancellationToken token)
 {
  await gate.WaitAsync(token);
  try { return await Task.Run(()=>Load(path,token),token); } finally { gate.Release(); }
 }
 private static ImageDocument Load(string path,CancellationToken token)
 {
  token.ThrowIfCancellationRequested();
  if(new FileInfo(path).Length>128L*1024*1024) throw new InvalidDataException(L.Text("FileTooLarge"));
  _=configuration.Value;
  string extension=Path.GetExtension(path).ToLowerInvariant();
  using(var input=File.OpenRead(path)) { Span<byte> h=stackalloc byte[8]; int n=input.ReadAtLeast(h,h.Length,throwOnEndOfStream:false); if(n>=3&&h[0]==255&&h[1]==216&&h[2]==255) extension=".jpg"; else if(n==8&&h.SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})) extension=".png"; else if(n>=6&&(h[..6].SequenceEqual("GIF87a"u8)||h[..6].SequenceEqual("GIF89a"u8))) extension=".gif"; else if(n>=2&&h[0]==66&&h[1]==77) extension=".bmp"; }
  bool animated=extension==".gif";
  var settings=new MagickReadSettings { Format=extension switch { ".jpg" or ".jpeg"=>MagickFormat.Jpeg,".png"=>MagickFormat.Png,".bmp"=>MagickFormat.Bmp,".gif"=>MagickFormat.Gif,".heic" or ".heif"=>MagickFormat.Heic,".webp"=>MagickFormat.WebP,".avif"=>MagickFormat.Avif,".dib"=>MagickFormat.Dib,".tif" or ".tiff"=>MagickFormat.Tiff,".ico"=>MagickFormat.Ico,".svg"=>MagickFormat.Msvg,_=>throw new InvalidDataException(L.Text("UnsupportedFormat")) } };
  if(!animated&&extension!=".ico") { settings.FrameIndex=0; settings.FrameCount=1; }
  byte[]? svg=extension==".svg"?SvgInput.Read(path):null;
  using var images=new MagickImageCollection();
  if(svg==null) images.Ping(path,settings); else images.Ping(svg,settings);
  if(images.Count==0) throw new InvalidDataException(L.Text("NoImages"));
  long cw=0,ch=0;
  foreach(var image in images) { CheckSize(image.Width,image.Height); cw=Math.Max(cw,Math.Max(image.Width,image.Page.Width)); ch=Math.Max(ch,Math.Max(image.Height,image.Page.Height)); }
  if(animated&&checked(cw*ch*images.Count)>24_000_000) throw new InvalidDataException(L.Text("GifTooLarge"));
  if(extension==".ico") { settings.FrameIndex=(uint)Enumerable.Range(0,images.Count).MaxBy(i=>(long)images[i].Width*images[i].Height); settings.FrameCount=1; }
  token.ThrowIfCancellationRequested();
  if(svg==null) images.Read(path,settings); else images.Read(svg,settings);
  uint iterations=images[0].AnimationIterations;
  if(animated) images.Coalesce(); else images[0].AutoOrient();
  var frames=new List<ImageFrame>();
  try {
   foreach(var image in images) {
    token.ThrowIfCancellationRequested(); CheckSize(image.Width,image.Height);
    if(image.GetColorProfile()!=null)image.TransformColorSpace(ColorProfiles.SRGB);else image.ColorSpace=ColorSpace.sRGB;
    using var pixels=image.GetPixels(); byte[] bytes=pixels.ToByteArray(PixelMapping.BGRA)??throw new InvalidDataException(L.Text("PixelsUnreadable"));
    var bitmap=new WriteableBitmap(new PixelSize((int)image.Width,(int)image.Height),new Vector(96,96),PixelFormat.Bgra8888,AlphaFormat.Unpremul);
    using(var buffer=bitmap.Lock()) { for(int y=0;y<(int)image.Height;y++) Marshal.Copy(bytes,y*(int)image.Width*4,buffer.Address+y*buffer.RowBytes,(int)image.Width*4); }
    double ms=image.AnimationDelay*1000d/(image.AnimationTicksPerSecond==0?100:image.AnimationTicksPerSecond);
    frames.Add(new(bitmap,TimeSpan.FromMilliseconds(ms<20?100:ms),animated?null:bytes));
   }
   return new(frames,iterations);
  } catch { foreach(var frame in frames) frame.Bitmap.Dispose(); throw; }
 }
 private static void CheckSize(uint width,uint height) { if(width==0||height==0||(long)width*height>40_000_000) throw new InvalidDataException(L.Text("ImageTooLarge")); }
}
