using System.Text.RegularExpressions;
namespace Luma.Services;
public static class FolderNavigator
{
 public static readonly string[] Extensions=[".jpg",".jpeg",".png",".gif",".heic",".heif",".webp",".avif",".bmp",".dib",".tif",".tiff",".ico",".svg"];
 public const string FormatNames="JPEG · PNG · GIF · HEIC/HEIF · WebP · AVIF · BMP/DIB · TIFF · ICO · SVG";
 public static bool Supported(string path)=>Extensions.Contains(Path.GetExtension(path),StringComparer.OrdinalIgnoreCase);
 public static string[] Scan(string path,CancellationToken token)
 {
  var files=new List<string>(); foreach(var file in Directory.EnumerateFiles(Path.GetDirectoryName(path)!)) { token.ThrowIfCancellationRequested(); if(Supported(file)) files.Add(file); }
  files.Sort((a,b)=>Compare(Path.GetFileName(a),Path.GetFileName(b))); return files.ToArray();
 }
 private static int Compare(string a,string b)
 {
  var aa=Regex.Split(a,"([0-9]+)"); var bb=Regex.Split(b,"([0-9]+)");
  for(int i=0;i<Math.Min(aa.Length,bb.Length);i++) { string x=aa[i],y=bb[i]; int c; if(i%2==1) { x=x.TrimStart('0');y=y.TrimStart('0');c=x.Length.CompareTo(y.Length);if(c==0)c=string.CompareOrdinal(x,y); } else c=string.Compare(x,y,StringComparison.CurrentCultureIgnoreCase); if(c!=0)return c; }
  int count=aa.Length.CompareTo(bb.Length);return count!=0?count:string.CompareOrdinal(a,b);
 }
}
