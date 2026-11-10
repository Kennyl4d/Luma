using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Luma;
using Luma.Services;
using Luma.Ocr;
using Luma.Imaging;

internal static class Program
{
    static int passed;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object? Call(object target, string method, params object?[] args) => target.GetType().GetMethod(method, Private)!.Invoke(target,args);
    static T Field<T>(object target,string name) => (T)target.GetType().GetField(name,Private)!.GetValue(target)!;
    static void Check(bool value,string name) { if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);passed++; }
    static void Pump() { var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame); }
    [STAThread] static int Main()
    {
        try { Run();return 0; } catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
    static void CheckOcr(string expected, int width, int height, double fontSize, double angle)
    {
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            drawing.PushTransform(new RotateTransform(angle, width / 2d, height / 2d));
            var text = new FormattedText(expected, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), fontSize, Brushes.Black, 1);
            drawing.DrawText(text, new Point((width - text.Width) / 2, (height - text.Height) / 2));
            drawing.Pop();
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();

        var task = new OcrService().RecognizeAsync(bitmap, "it", CancellationToken.None);
        while (!task.IsCompleted) Pump();
        var result = task.GetAwaiter().GetResult();
        var overlay = new TextSelectionOverlay();
        overlay.SetText(result);
        Check(overlay.SelectedText(all: true) == expected, $"Offline OCR recognizes {expected}");
        Check(result.Words.Count == expected.Split(' ').Length && result.Words.All(word =>
            new Rect(0, 0, width, height).Contains(word.Bounds)), "OCR returns one bounded selection per word");
    }

    static void Run()
    {
        var app=new App { ShutdownMode=ShutdownMode.OnExplicitShutdown };
        app.InitializeComponent();
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        L.Refresh();
        CheckOcr("INGRESSO PRINCIPALE", 800, 600, 30, -24);
        CheckOcr("Importo totale 49,90 euro", 3000, 1100, 15, 0);
        var window=new MainWindow(null){ShowInTaskbar=false,WindowStartupLocation=WindowStartupLocation.Manual,Left=-20000,Top=-20000};
        window.Show();Pump();
        // Use a generated image so no personal files enter regression tests.
        var bitmap=BitmapSource.Create(2400,1800,96,96,PixelFormats.Gray8,null,new byte[2400*1800],2400);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        string path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"luma-regression-"+Guid.NewGuid()+".png");
        using(var stream=System.IO.File.Create(path))encoder.Save(stream);
        try
        {
            var open=(Task)Call(window,"OpenAsync",path,true)!;
            while(!open.IsCompleted)Pump();open.GetAwaiter().GetResult();Pump();
            var viewport=(ScrollViewer)window.FindName("Viewport");
            var surface=(FrameworkElement)window.FindName("ImageSurface");
            var overlay=(TextSelectionOverlay)window.FindName("TextOverlay");
            Check(!Field<bool>(window,"ocrEnabled")&&!overlay.IsHitTestVisible&&!overlay.HasText,"OCR disabled on image open");
            Check(((Button)window.FindName("OcrButton")).IsEnabled,"OCR available for still image");
            foreach(int rotation in new[]{0,90,180,270})
            {
                ((RotateTransform)surface.LayoutTransform).Angle=rotation;
                Call(window,"Fit");Pump();
                for(int i=0;i<7;i++)
                {
                    var anchor=new Point(viewport.ViewportWidth*.68,viewport.ViewportHeight*.59);
                    var old=viewport.TranslatePoint(anchor,surface);
                    double before=Field<double>(window,"zoom");
                    Call(window,"ChangeZoom",1.25,(Point?)anchor);Pump();
                    double ratio=Field<double>(window,"zoom")/before;
                    var moved=surface.TranslatePoint(new Point(old.X*ratio,old.Y*ratio),viewport);
                    // Once both axes scroll, neither axis needs edge clamping.
                    if(viewport.ScrollableWidth>100&&viewport.ScrollableHeight>100)
                        Check((moved-anchor).Length<1,$"Pointer anchor at {rotation} degrees, step {i}: drift={(moved-anchor).Length:F3}");
                }
                for(int i=0;i<3;i++)
                {
                    var anchor=new Point(viewport.ViewportWidth*.58,viewport.ViewportHeight*.54);
                    var old=viewport.TranslatePoint(anchor,surface);double before=Field<double>(window,"zoom");
                    Call(window,"ChangeZoom",1/1.25,(Point?)anchor);Pump();
                    double ratio=Field<double>(window,"zoom")/before;
                    var moved=surface.TranslatePoint(new Point(old.X*ratio,old.Y*ratio),viewport);
                    if(viewport.ScrollableWidth>100&&viewport.ScrollableHeight>100)
                        Check((moved-anchor).Length<1,$"Zoom-out anchor at {rotation} degrees: drift={(moved-anchor).Length:F3}");
                }
            }
            // Avoid invoking the native OCR engine: inject an overlay and test clearing it.
            window.GetType().GetField("ocrEnabled",Private)!.SetValue(window,true);
            overlay.SetText(new RecognizedText(2400,1800,new[]{new RecognizedWord("test",new Rect(10,10,100,50),0)}));
            overlay.IsHitTestVisible=true;
            Call(window,"OcrClick",window,new RoutedEventArgs());Pump();
            Check(!Field<bool>(window,"ocrEnabled")&&!overlay.HasText&&!overlay.IsHitTestVisible,"Disabling OCR clears invisible hit targets");
            window.GetType().GetField("ocrEnabled",Private)!.SetValue(window,true);
            open=(Task)Call(window,"OpenAsync",path,true)!;while(!open.IsCompleted)Pump();open.GetAwaiter().GetResult();Pump();
            Check(!Field<bool>(window,"ocrEnabled")&&!overlay.HasText,"Opening another image resets OCR");
            var about=new AboutWindow(false);Check(about.FindName("CheckUpdatesButton") is Button,"About update action loads");about.Close();
            var largePath=LargeImageFixture.Create();
            try
            {
                Check(new System.IO.FileInfo(largePath).Length>128L*1024*1024,"Fixture exceeds former 128 MiB limit");
                var largeTask=new ImageLoader().LoadAsync(largePath,CancellationToken.None);
                while(!largeTask.IsCompleted)Pump();
                var large=largeTask.GetAwaiter().GetResult();
                Check(large.Frames[0].Bitmap.PixelWidth==9000&&large.Frames[0].Bitmap.PixelHeight==6000,"54 MP / 154 MiB BMP opens through Windows decoder");
                string tiff=largePath+".tiff";
                try
                {
                    using(var source=new ImageMagick.MagickImage(largePath))source.Write(tiff);
                    var nativeTask=new ImageLoader().LoadAsync(tiff,CancellationToken.None);
                    while(!nativeTask.IsCompleted)Pump();
                    var native=nativeTask.GetAwaiter().GetResult();
                    Check(native.Frames[0].Bitmap.PixelWidth==9000&&native.Frames[0].Bitmap.PixelHeight==6000,"54 MP TIFF opens through native decoder beyond old memory cap");
                }
                finally { System.IO.File.Delete(tiff); }
            }
            finally { System.IO.File.Delete(largePath); }
        }
        finally { window.Close();System.IO.File.Delete(path);app.Shutdown(); }
        Console.WriteLine($"{passed} viewer checks passed.");
    }
}
