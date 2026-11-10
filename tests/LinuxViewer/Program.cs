using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Luma;
using Luma.Ocr;
using SkiaSharp;

internal static class Program
{
    static int passed;
    static double Distance(Point a,Point b)=>Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2));
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static object? Call(object target,string method,params object?[] args)=>target.GetType().GetMethod(method,Private)!.Invoke(target,args);
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private)!.GetValue(target)!;
    static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);passed++;}
    static void Pump()=>Dispatcher.UIThread.RunJobs();
    [STAThread] static int Main()
    {
        try { AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();Run();return 0; }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
    static void CheckOfflineOcr()
    {
        const string expected = "INGRESSO PRINCIPALE";
        string path = Path.Combine(Path.GetTempPath(), $"luma-ocr-{Guid.NewGuid()}.png");
        try
        {
            using var bitmap = new SKBitmap(800, 600);
            using var canvas = new SKCanvas(bitmap);
            using var typeface = SKTypeface.FromFamilyName("DejaVu Sans");
            using var font = new SKFont(typeface, 30);
            using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            canvas.Clear(SKColors.White);
            canvas.RotateDegrees(-24, 400, 300);
            canvas.DrawText(expected, 400, 300, SKTextAlign.Center, font, paint);
            using (var stream = File.Create(path))
                bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);

            var load = new Luma.Imaging.ImageLoader().LoadAsync(path, CancellationToken.None);
            while (!load.IsCompleted) Pump();
            using var image = load.GetAwaiter().GetResult();
            var recognize = new OcrService().RecognizeAsync(image.Frames[0], "it", CancellationToken.None);
            while (!recognize.IsCompleted) Pump();
            var overlay = new TextSelectionOverlay();
            overlay.SetText(recognize.GetAwaiter().GetResult());
            Check(overlay.SelectedText(all: true) == expected, "Offline OCR recognizes tilted text");
        }
        finally { File.Delete(path); }
    }

    static void Run()
    {
        CheckOfflineOcr();
        var window=new MainWindow();window.Show();Pump();
        string path=LargeImageFixture.Create(2400,1800);
        try
        {
            var probe=new Luma.Imaging.ImageLoader().LoadAsync(path,CancellationToken.None);

            while(!probe.IsCompleted)Pump();
            using(var decoded=probe.GetAwaiter().GetResult())Check(decoded.Frames.Count==1,"Headless decoder fixture");
            var open=(Task)Call(window,"OpenAsync",path,true)!;while(!open.IsCompleted)Pump();open.GetAwaiter().GetResult();Pump();
            var viewport=window.FindControl<ScrollViewer>("Viewport")!;
            var surface=window.FindControl<Grid>("ImageSurface")!;
            var transform=window.FindControl<LayoutTransformControl>("TransformSurface")!;
            var overlay=window.FindControl<TextSelectionOverlay>("TextOverlay")!;
            Check(Field<Luma.Imaging.ImageDocument?>(window,"document")!=null,"Image opened: "+window.FindControl<TextBlock>("Message")!.Text);
            Check(!Field<bool>(window,"ocrEnabled")&&!overlay.IsHitTestVisible&&!overlay.HasText,"OCR disabled on image open");
            Check(window.FindControl<Button>("OcrButton")!.IsEnabled,"OCR available for still image");
            foreach(int rotation in new[]{0,90,180,270})
            {
                ((RotateTransform)transform.LayoutTransform!).Angle=rotation;Call(window,"Fit");Pump();
                for(int i=0;i<7;i++)
                {
                    var anchor=new Point(viewport.Viewport.Width*.68,viewport.Viewport.Height*.59);
                    var old=viewport.TranslatePoint(anchor,surface)!.Value;double oldWidth=surface.Bounds.Width,oldHeight=surface.Bounds.Height;
                    Call(window,"ChangeZoom",1.25,(Point?)anchor);Pump();

                    var moved=surface.TranslatePoint(new Point(old.X*surface.Bounds.Width/oldWidth,old.Y*surface.Bounds.Height/oldHeight),viewport)!.Value;
                    if(viewport.Offset.X>1&&viewport.Offset.Y>1&&viewport.Offset.X<viewport.Extent.Width-viewport.Viewport.Width-1&&viewport.Offset.Y<viewport.Extent.Height-viewport.Viewport.Height-1)
                        Check(Distance(moved,anchor)<1,$"Pointer anchor at {rotation} degrees, step {i}: drift={Distance(moved,anchor):F3}");
                }
                for(int i=0;i<3;i++)
                {
                    var anchor=new Point(viewport.Viewport.Width*.58,viewport.Viewport.Height*.54);
                    var old=viewport.TranslatePoint(anchor,surface)!.Value;double oldWidth=surface.Bounds.Width,oldHeight=surface.Bounds.Height;
                    Call(window,"ChangeZoom",1/1.25,(Point?)anchor);Pump();

                    var moved=surface.TranslatePoint(new Point(old.X*surface.Bounds.Width/oldWidth,old.Y*surface.Bounds.Height/oldHeight),viewport)!.Value;
                    if(viewport.Offset.X>1&&viewport.Offset.Y>1&&viewport.Offset.X<viewport.Extent.Width-viewport.Viewport.Width-1&&viewport.Offset.Y<viewport.Extent.Height-viewport.Viewport.Height-1)
                        Check(Distance(moved,anchor)<1,$"Zoom-out anchor at {rotation} degrees: drift={Distance(moved,anchor):F3}");
                }
            }
            window.GetType().GetField("ocrEnabled",Private)!.SetValue(window,true);
            overlay.SetText(new RecognizedText(2400,1800,new[]{new RecognizedWord("test",new Rect(10,10,100,50),0)}));overlay.IsHitTestVisible=true;
            Call(window,"OcrClick",window,new RoutedEventArgs());Pump();
            Check(!Field<bool>(window,"ocrEnabled")&&!overlay.HasText&&!overlay.IsHitTestVisible,"Disabling OCR clears invisible hit targets");
            window.GetType().GetField("ocrEnabled",Private)!.SetValue(window,true);
            open=(Task)Call(window,"OpenAsync",path,true)!;while(!open.IsCompleted)Pump();open.GetAwaiter().GetResult();Pump();
            Check(!Field<bool>(window,"ocrEnabled")&&!overlay.HasText,"Opening another image resets OCR");
            var about=new AboutWindow();Check(about.FindControl<Button>("CheckUpdatesButton")!=null,"About update action loads");about.Close();
            var largePath=LargeImageFixture.Create();
            try
            {
                Check(new FileInfo(largePath).Length>128L*1024*1024,"Fixture exceeds former 128 MiB limit");
                var largeTask=new Luma.Imaging.ImageLoader().LoadAsync(largePath,CancellationToken.None);
                while(!largeTask.IsCompleted)Pump();
                using var large=largeTask.GetAwaiter().GetResult();
                Check(large.Frames[0].Bitmap.PixelSize.Width==9000&&large.Frames[0].Bitmap.PixelSize.Height==6000,"54 MP / 154 MiB BMP opens through Linux loader");
            }
            finally { File.Delete(largePath); }
        }
        finally{window.Close();File.Delete(path);}
        Console.WriteLine($"{passed} viewer checks passed.");
    }
}
