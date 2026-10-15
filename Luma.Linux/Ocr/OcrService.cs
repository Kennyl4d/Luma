using Avalonia;
using Luma.Imaging;
using System.Runtime.InteropServices;
namespace Luma.Ocr;
public sealed record RecognizedWord(string Text,Rect Bounds,int Line);
public sealed record RecognizedText(int Width,int Height,IReadOnlyList<RecognizedWord> Words);
public sealed class OcrService
{
 private readonly SemaphoreSlim gate=new(1);
 public async Task<RecognizedText> RecognizeAsync(ImageFrame frame,string language,CancellationToken token)
 { int width=frame.Bitmap.PixelSize.Width,height=frame.Bitmap.PixelSize.Height;byte[] pixels=frame.Pixels??throw new InvalidOperationException();await gate.WaitAsync(token);try{return await Task.Run(()=>Recognize(width,height,pixels,language,token),token);}finally{gate.Release();} }
 private static unsafe RecognizedText Recognize(int sw,int sh,byte[] pixels,string language,CancellationToken token)
 {
  token.ThrowIfCancellationRequested();double scale=Math.Min(1,2200d/Math.Max(sw,sh));int w=Math.Max(1,(int)(sw*scale)),h=Math.Max(1,(int)(sh*scale));var rgb=new byte[w*h*3];
  for(int y=0;y<h;y++){
   token.ThrowIfCancellationRequested();double fy=Math.Clamp((y+.5)*sh/h-.5,0,sh-1);int y0=(int)fy,y1=Math.Min(sh-1,y0+1);double wy=fy-y0;
   for(int x=0;x<w;x++){
    double fx=Math.Clamp((x+.5)*sw/w-.5,0,sw-1);int x0=(int)fx,x1=Math.Min(sw-1,x0+1);double wx=fx-x0;int a=(y0*sw+x0)*4,b=(y0*sw+x1)*4,c=(y1*sw+x0)*4,d=(y1*sw+x1)*4,t=(y*w+x)*3;
    for(int channel=0;channel<3;channel++){int color=2-channel;double top=Composite(pixels,a,color)*(1-wx)+Composite(pixels,b,color)*wx,bottom=Composite(pixels,c,color)*(1-wx)+Composite(pixels,d,color)*wx;rgb[t+channel]=(byte)Math.Clamp(Math.Round(top*(1-wy)+bottom*wy),0,255);}
   }
  }
  string model=language switch {"cs"=>"ces","de"=>"deu","es"=>"spa","fr"=>"fra","it"=>"ita","ja"=>"jpn","ko"=>"kor","pl"=>"pol","pt-BR"=>"por","ru"=>"rus","tr"=>"tur","zh-Hans"=>"chi_sim","zh-Hant"=>"chi_tra",_=>"eng"};
  if(Environment.GetEnvironmentVariable("OMP_THREAD_LIMIT")==null)Environment.SetEnvironmentVariable("OMP_THREAD_LIMIT","2");
  IntPtr api=Native.Create(),iterator=IntPtr.Zero;
  try {
   if(api==IntPtr.Zero||Native.Init(api,Path.Combine(AppContext.BaseDirectory,"tessdata"),model=="eng"?"eng":model+"+eng")!=0)throw new InvalidOperationException("Tesseract initialization failed");
   Native.SetVariable(api,"user_defined_dpi","150");Native.SetPageMode(api,11);
   fixed(byte* image=rgb){Native.SetImage(api,(IntPtr)image,w,h,3,w*3);if(Native.Recognize(api,IntPtr.Zero)!=0)throw new InvalidOperationException("Tesseract recognition failed");}
   token.ThrowIfCancellationRequested();iterator=Native.GetIterator(api);var words=new List<RecognizedWord>();if(iterator==IntPtr.Zero)return new(sw,sh,words);
   IntPtr page=Native.GetPageIterator(iterator);Native.Begin(page);int line=-1;
   do{token.ThrowIfCancellationRequested();if(Native.IsBeginning(page,2)!=0)line++;IntPtr utf8=Native.GetText(iterator,3);string text="";if(utf8!=IntPtr.Zero){try{text=Marshal.PtrToStringUTF8(utf8)?.Trim()??"";}finally{Native.DeleteText(utf8);}}
    if(text.Length>0&&Native.Confidence(iterator,3)>=35&&Native.Box(page,3,out int x1,out int y1,out int x2,out int y2)!=0)words.Add(new(text,new Rect(x1*(double)sw/w,y1*(double)sh/h,(x2-x1)*(double)sw/w,(y2-y1)*(double)sh/h),line));
   }while(Native.Next(iterator,3)!=0);return new(sw,sh,words);
  }finally{if(iterator!=IntPtr.Zero)Native.DeleteIterator(iterator);if(api!=IntPtr.Zero)Native.Delete(api);}
 }
 private static double Composite(byte[] pixels,int offset,int channel)=>(pixels[offset+channel]*pixels[offset+3]+255*(255-pixels[offset+3]))/255d;
 private static class Native
 {
  const string Lib="libtesseract.so.5";
  [DllImport(Lib,EntryPoint="TessBaseAPICreate")]internal static extern IntPtr Create();
  [DllImport(Lib,EntryPoint="TessBaseAPIDelete")]internal static extern void Delete(IntPtr api);
  [DllImport(Lib,EntryPoint="TessBaseAPIInit3")]internal static extern int Init(IntPtr api,[MarshalAs(UnmanagedType.LPUTF8Str)]string path,[MarshalAs(UnmanagedType.LPUTF8Str)]string languages);
  [DllImport(Lib,EntryPoint="TessBaseAPISetVariable")]internal static extern int SetVariable(IntPtr api,[MarshalAs(UnmanagedType.LPUTF8Str)]string name,[MarshalAs(UnmanagedType.LPUTF8Str)]string value);
  [DllImport(Lib,EntryPoint="TessBaseAPISetPageSegMode")]internal static extern void SetPageMode(IntPtr api,int mode);
  [DllImport(Lib,EntryPoint="TessBaseAPISetImage")]internal static extern void SetImage(IntPtr api,IntPtr data,int width,int height,int bytesPerPixel,int bytesPerLine);
  [DllImport(Lib,EntryPoint="TessBaseAPIRecognize")]internal static extern int Recognize(IntPtr api,IntPtr monitor);
  [DllImport(Lib,EntryPoint="TessBaseAPIGetIterator")]internal static extern IntPtr GetIterator(IntPtr api);
  [DllImport(Lib,EntryPoint="TessResultIteratorGetPageIterator")]internal static extern IntPtr GetPageIterator(IntPtr iterator);
  [DllImport(Lib,EntryPoint="TessPageIteratorBegin")]internal static extern void Begin(IntPtr iterator);
  [DllImport(Lib,EntryPoint="TessPageIteratorIsAtBeginningOf")]internal static extern int IsBeginning(IntPtr iterator,int level);
  [DllImport(Lib,EntryPoint="TessPageIteratorBoundingBox")]internal static extern int Box(IntPtr iterator,int level,out int left,out int top,out int right,out int bottom);
  [DllImport(Lib,EntryPoint="TessResultIteratorGetUTF8Text")]internal static extern IntPtr GetText(IntPtr iterator,int level);
  [DllImport(Lib,EntryPoint="TessResultIteratorConfidence")]internal static extern float Confidence(IntPtr iterator,int level);
  [DllImport(Lib,EntryPoint="TessResultIteratorNext")]internal static extern int Next(IntPtr iterator,int level);
  [DllImport(Lib,EntryPoint="TessResultIteratorDelete")]internal static extern void DeleteIterator(IntPtr iterator);
  [DllImport(Lib,EntryPoint="TessDeleteText")]internal static extern void DeleteText(IntPtr text);
 }
}
